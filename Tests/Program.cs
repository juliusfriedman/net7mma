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

using Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Media.UnitTests
{

    public class Program
    {
        /// <summary>
        /// A format for the output which occurs when unit testing.
        /// </summary>
        internal static string TestingFormat = "{0}:=>{1}"; 

        /// <summary>
        /// The UnitTests which will be run to test the implemenation logic
        /// </summary>
        static Action[] LogicTests = new Action[] { 
            //Experimental Classes (Should not be used in real code)
            TestBus,
            TestStopWatch,
            TestClock,
            TestTimer,
            //TestSocketConfiguration,
            TestIPNetworkConnection,
            TestMachine,
            TestEncodingExtensions,
            TestUtility, 
            TestBinary, 
            //Rtp / Rtcp
            TestRtpRtcp, 
            // Frame Level
            TestRtpFrame, 
            // JPEG
            TestRFC2435Frame, 
            // MPEG
            TestRFC3640AudioFrame, 
            //Tools 
            TestRtpTools, 
            //Containers
            TestContainerImplementations, 
            //Sdp
            TestSdp, 
            //HttpMessage
            TestHttpMessage, 
            //RtspMessage
            TestRtspMessage, 
            //RtpClient
            TestRtpClient,
            Media.UnitTests.RtpClientUnitTests.TestProcessFrameData.BackToBackRtspMessages, 
            Media.UnitTests.RtpClientUnitTests.TestProcessFrameData.Issue17245_Case1_Iteration, 
            Media.UnitTests.RtpClientUnitTests.TestProcessFrameData.Issue17245_Case2_Iteration,
            Media.UnitTests.RtpClientUnitTests.TestInterleavedFraming,           
            //RtspClient
        };

        /// <summary>
        /// This is where the Tests.Program is currently running.
        /// </summary>
        static string executingAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// The entry point of the unit testing application
        /// </summary>
        /// <param name="args"></param>
        [MTAThread]
        public static void Main(string[] args)
        {

            Console.WriteLine();

            //Run the main tests
            foreach (Action test in LogicTests) RunTest(test);

            Console.WriteLine("Logic Tests Complete! Press Q to Exit or any other key to perform the live tests.");

            if (Console.ReadKey(true).Key == ConsoleKey.Q) return;

            RunTest(HttpClientTests);

            RunTest(RtspClientTests);

            RunTest(RtspInspector);

            RunTest(TestServer);
        }

        #region Unit Tests

        #region Not Yet Redone

        //The following tests need to be properly seperated

        public static void TestUtility()
        {

            //Each octet reflects it's offset in hexidecimal
            // 0 - 9 in hex is the same as decimal
            byte[] haystack = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15 },
                //Something to look for in the above
                   needle = new byte[] { 0x14, 0x15 };

            //For all 20 bytes look for them in the ensure haystack starting at the beginning
            for (int offset = 0, test = 0, haystackLength = haystack.Length, count = haystackLength, needleBegin = 0, needleLength = needle.Length;
                //Perform tests up to the highest value in haystack by design
                test < haystackLength - 1; ++test, offset = 0, count = haystackLength, needle[1] = (byte)test, needle[0] = (byte)(test - 1)) //increment the test and reset the offset each and count each time
            {
                //Look for the whole needle in the haystack
                int offsetAfterParsing = Utility.ContainsBytes(haystack, ref offset, ref count, needle, needleBegin, needleLength);

                //Get the pointer to the bytes which correspond to the match in total
                var match = haystack.Skip(offset > 0 ? offset : 0).Take(needleLength);

                //Check for an invalid result in the test
                if (!match.SequenceEqual(needle)
                            ||//Double check the result is valid by examining the first byte to be equal to the offset (which is by design)                        
                                match.First() != offset) throw new Exception("Invalid result found!");
                ///////////////////////////////////////////////////                
                //Write the info about the test to show progress
                else Console.WriteLine(string.Format(TestingFormat, "FoundBytes Found", "@" + offset + " : " + BitConverter.ToString(match.ToArray())));

                //If not at the end of the haystack try to perform the same test in a different way
                if (offsetAfterParsing < haystackLength &&
                    //The first time no search will occur because the end based on offset is already past
                    //Search again, assign offsetAfterParsing which should waste the rest of the haystack and should not be found (which is by design)
                    (offsetAfterParsing = Utility.ContainsBytes(haystack, ref offset, ref count, needle, needleBegin, needleLength)) > 0 && offsetAfterParsing - 1 != offset) throw new Exception("Reading from the same offset produced a different result");
            }
        }


        static string[] PublicRtspHosts = new string[]
        {
            "rtsp://streamreader:trudat55@69.84.126.216:88/videoMain",
            //
            "rtsp://107.215.20.171/PSIA/Streaming/channels/h264",
            //Darwin RTSP (Sorenson Audio) (MPEG 4 Video)
            "rtsp://quicktime.uvm.edu:1554/waw/wdi05hs2b.mov",  //Single media item
            //Hexlix RTSP (H264 Video)
            "rtsp://46.249.213.93/broadcast/gamerushtv-tablet.3gp", //Continious Media
            //Wowza RTSP (H264 Video)
            "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov",  //Single media item
            //GoogleStreamer (Udp only) IPv6 if available
            "rtsp://v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/video.3gp", //Single media item
            "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp", //Single media item
            //GrandStream
            "rtsp://avollmar.dyndns.org:3030/",//Continious Media
            //Hikvision
            "rtsp://1:1@118.70.181.233:2134/PSIA/Streamingchannels/0",//Continious Media
            "rtsp://1:1@118.70.181.233:2114/PSIA/Streamingchannels/0",//Continious Media
            //Panasonic  (H264 Video)[1920x1080 @ 30 fps]
            "rtsp://118.70.125.33/mediainput/h264", //Continious Media (Agilon Onvif)
            "rtsp://118.70.125.33:20554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:21554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:22554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:23554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:24554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:25554/mediainput/h264", //Continious Media
            "rtsp://118.70.125.33:26554/mediainput/h264", //Continious Media
            //Oem  (H264 Video)[1280x960 @ 30 fps]
            "rtsp://admin:12345@118.70.125.33:12554/cam/realmonitor?channel=1&subtype=0", //Continious Media
            "rtsp://admin:12345@118.70.125.33:13554/cam/realmonitor?channel=1&subtype=0", //Continious Media
            //Benco
            "rtsp://118.70.125.33:35554/user=admin&password=&channel=1&stream=0.sdp?real_stream", //Continious Media
            "rtsp://118.70.125.33:36554/user=admin&password=&channel=1&stream=0.sdp?real_stream", //Continious Media
            "rtsp://118.70.125.33:37554/user=admin&password=&channel=1&stream=0.sdp?real_stream", //Continious Media
            "rtsp://118.70.125.33:38554/user=admin&password=&channel=1&stream=0.sdp?real_stream", //Continious Media
            //Foscam
            "rtsp://hptvn:hptvn@hptvn-com.dyndns.org:9821/videoMain", //Continious Media
            "rtsp://hptvn:hptvn@hptvn-com.dyndns.org:9826/videoMain", //Continious Media
            "rtsp://hptvn:hptvn@hptvn-com.dyndns.org:9831/videoMain", //Continious Media
            //AvTech
            "rtsp://demo:demo@sieuthivienthong.dyndns.org:8081/live/h264", //Continious Media (TCP Re-transmission happen frequently)
            "rtsp://admin:admin@avm561.ddns.eagleeyes.tw:161/live/h264", //Continious Media (Host only allows 1 connection every few minutes)
            //Lilin
            "rtsp://admin:pass@118.70.125.33:27554/rtsph2641080p", //Continious Media
            //Arecont
            "rtsp://admin:admin@118.70.125.33:28554/h264.sdp?res=full", //Continious Media
            //Real RTSP
            "rtsp://164.107.27.156:554/media/medvids/drape_positions.rm",
            "rtsp://dl.lib.brown.edu:554/areserves/1093545294660883.mp3",
             //MS-RTSP (MJPEG Video) (WMA2 Audio)
            "rtsp://videozones.francetv.fr/france-dom-tom/Autre/Autre/2012/S01/J5/366723_envoyespecial_sujet3_20120105.wmv",//Single media item
            //MS-RTSP ASF wma2 wmv1
            "rtsp://granton.ucs.ed.ac.uk/domsdemo/v2003-1.wmv",//Single media item
            //MS-RTSP ASF wma2 wmv3
            "rtsp://www.reelgood.tv/reelgoodtv",
        };


        public static void RtspClientTests()
        {
            Media.Rtsp.RtspClient.ClientProtocolType? proto = null;

            foreach (string host in PublicRtspHosts)
            {

            TestStart:

                try
                {
                    TestRtspClient(host, default(System.Net.NetworkCredential), proto);
                }
                catch (Exception ex)
                {
                    writeError(ex);
                }

                Console.WriteLine("Done. (T) to test again forcing TCP, (U) to test again forcing UDP Press (W) to run the same test again, Press (Q) or anything else to progress to the next test.");

                ConsoleKey next = Console.ReadKey(true).Key;

                switch (next)
                {
                    case ConsoleKey.U: { proto = Media.Rtsp.RtspClient.ClientProtocolType.Udp; goto case ConsoleKey.W; }
                    case ConsoleKey.T: { proto = Media.Rtsp.RtspClient.ClientProtocolType.Tcp; goto case ConsoleKey.W; }
                    default:
                    case ConsoleKey.Q: continue;
                    case ConsoleKey.W: goto TestStart;
                }
            }
        }

        static void TestRtpClient()
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.RtpClientUnitTests));

            TestRtpClient(false);

            TestRtpClient(true);

            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.BackgroundColor = ConsoleColor.Black;
        }

        /// <summary>
        /// Tests the RtpClient.
        /// </summary>
        static void TestRtpClient(bool tcp = true)
        {

            //Notes need to set EndTime on each context.

            //Should re-write to use FromSessionDescription methods for cleaner test.

            //No tcp test right now.
            if (tcp) tcp = false;

            //Start a test to send a single frame as quickly as possible to a single party.
            //Deactivate after sending said frame test the disposable implementation, packets not yet received will be lost at that time.
            using (System.IO.TextWriter consoleWriter = new System.IO.StreamWriter(Console.OpenStandardOutput()))
            {

                //Get the local interface address
                System.Net.IPAddress localIp = Media.Common.Extensions.Socket.SocketExtensions.GetFirstV4IPAddress();

                //Using a sender, automatically create memory and fire events
                using (var sender = new Media.Rtp.RtpClient())
                {
                    //Create a Session Description
                    Media.Sdp.SessionDescription SessionDescription = new Media.Sdp.SessionDescription(1);

                    SessionDescription.Add(new Media.Sdp.SessionDescriptionLine("c=IN IP4 " + localIp.ToString()));

                    //Add a MediaDescription to our Sdp on any port 17777 for RTP/AVP Transport using the RtpJpegPayloadType
                    SessionDescription.Add(new Media.Sdp.MediaDescription(Media.Sdp.MediaType.video, 17777, (tcp ? "TCP/" : string.Empty) + Media.Rtp.RtpClient.RtpAvpProfileIdentifier, Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame.RtpJpegPayloadType));

                    sender.RtcpPacketSent += (s, p, t) => TryPrintClientPacket(s, false, p);
                    sender.RtcpPacketReceieved += (s, p, t) => TryPrintClientPacket(s, true, p);
                    sender.RtpPacketSent += (s, p, t) => TryPrintClientPacket(s, false, p);

                    //Using a receiver
                    using (var receiver = new Media.Rtp.RtpClient())
                    {

                        //Determine when the sender and receive should time out
                        //sender.InactivityTimeout = receiver.InactivityTimeout = TimeSpan.FromSeconds(7);

                        receiver.RtcpPacketSent += (s, p, t) => TryPrintClientPacket(s, false, p);
                        receiver.RtcpPacketReceieved += (s, p, t) => TryPrintClientPacket(s, true, p);
                        receiver.RtpPacketReceieved += (s, p, t) => TryPrintClientPacket(s, true, p);

                        //Create and Add the required TransportContext's

                        int sendersId = Media.RFC3550.Random32(Media.Rtcp.SendersReport.PayloadType), receiversId = sendersId + 1;

                        //Create two transport contexts, one for the sender and one for the receiver.
                        //The Id of the parties must be known in advance in this stand alone example. (A conference would support more then 1 participant)
                        Media.Rtp.RtpClient.TransportContext sendersContext = new Media.Rtp.RtpClient.TransportContext(0, 1, sendersId, SessionDescription.MediaDescriptions.First(), true, receiversId),
                            receiversContext = new Media.Rtp.RtpClient.TransportContext(0, 1, receiversId, SessionDescription.MediaDescriptions.First(), true, sendersId);

                        if (tcp) consoleWriter.WriteLine("TCP TEST");
                        else consoleWriter.WriteLine("UDP TEST");

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - " + sender.InternalId + " - Senders SSRC = " + sendersContext.SynchronizationSourceIdentifier);

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - " + receiver.InternalId + " - Recievers SSRC = " + receiversContext.SynchronizationSourceIdentifier);

                        receiver.TryAddContext(receiversContext);

                        sender.TryAddContext(sendersContext);

                        //For Tcp a higher level protocol such as RTSP / SIP usually sets things up.
                        //Stand alone is also possible a socket just has to be created to facilitate accepts
                        if (tcp)
                        {
                            //Make a socket for the sender to receive connections on
                            var sendersSocket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                            //Bind and listen
                            sendersSocket.Bind(new System.Net.IPEndPoint(localIp, 17777));
                            sendersSocket.Listen(1);

                            //Start to accept connections
                            var acceptResult = sendersSocket.BeginAccept(new AsyncCallback(iar =>
                            {
                                //Get the socket used
                                var acceptedSocket = sendersSocket.EndAccept(iar);

                                sendersContext.Initialize(acceptedSocket);

                                receiversContext.Initialize(acceptedSocket);

                                //Connect the sender
                                sender.Activate();

                                //Connect the reciver (On the `otherside`)
                                receiver.Activate();

                            }), null);

                            //Make a socket for the receiver to connect to the sender on.
                            var rr = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                            //Connect to the sender
                            rr.Connect(new System.Net.IPEndPoint(localIp, 17777));

                            //acceptedSocket is now rr

                            while (false == acceptResult.IsCompleted) { System.Threading.Thread.Sleep(0); }

                        }
                        else //For Udp a port should be found unless the MediaDescription indicates a specific port.
                        {
                            int incomingRtpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17777, false), rtcpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17778),
                            ougoingRtpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10777, false), xrtcpPort = Media.Common.Extensions.Socket.SocketExtensions.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10778);

                            //Initialzie the sockets required and add the context so the RtpClient can maintin it's state, once for the receiver and once for the sender in this example.
                            //Most application would only have one or the other.

                            receiversContext.Initialize(localIp, localIp, incomingRtpPort, rtcpPort, ougoingRtpPort, xrtcpPort);
                            sendersContext.Initialize(localIp, localIp, ougoingRtpPort, xrtcpPort, incomingRtpPort, rtcpPort);

                            //Connect the sender
                            sender.Activate();

                            //Connect the reciver (On the `otherside`)
                            receiver.Activate();

                        }

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - Connection Established,  Encoding Frame");

                        //Make a frame
                        Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame testFrame = new Media.Rtsp.Server.MediaTypes.RFC2435Media.RFC2435Frame(new System.IO.FileStream(".\\Media\\JpegTest\\video.jpg", System.IO.FileMode.Open, System.IO.FileAccess.Read), 25, (int)sendersContext.SynchronizationSourceIdentifier, 0, (long)Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(DateTime.UtcNow));

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + "Sending Encoded Frame");

                        //Send it
                        sender.SendRtpFrame(testFrame);

                        sender.SendSendersReports();

                        //Wait for the senders report to be sent AND for the frame to be sent at least one time while the sender is connected
                        while (sender.IsActive && (sendersContext.SendersReport == null || false == sendersContext.SendersReport.Transferred.HasValue) || false == testFrame.Transferred) System.Threading.Thread.Sleep(0);

                        //Print the report information
                        if (sendersContext.SendersReport != null)
                        {
                            //Measure QoE / QoS based on sent / received ratio.
                            consoleWriter.WriteLine("\t Since : " + sendersContext.SendersReport.Transferred);
                            consoleWriter.WriteLine("\t -----------------------");
                            consoleWriter.WriteLine("\t Sender Sent : " + sendersContext.SendersReport.SendersPacketCount + " Packets");

                        }

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + "\t *** Sent RtpFrame, Sending Reports and Goodbye ***");

                        //Wait for a receivers report to be sent while the receiver is connected
                        while (receiver.IsActive && (receiversContext.ReceiversReport == null || false == receiversContext.ReceiversReport.Transferred.HasValue)) System.Threading.Thread.Sleep(0);

                        //Print the report information
                        if (receiversContext.ReceiversReport != null)
                        {
                            //Determine what is actually being received by obtaining the TransportContext of the receiver            
                            //In a real world program you would not have access to the receiversContext so you would look at the sendersContext.RecieverReport
                            consoleWriter.WriteLine("\t Since : " + receiversContext.ReceiversReport.Created);
                            consoleWriter.WriteLine("\t -----------------------");
                            consoleWriter.WriteLine("\t Receiver Received : " + receiver.TotalRtpPacketsReceieved + " RtpPackets");

                            //Write ReceptionReport information if contained
                            foreach (Media.Rtcp.ReportBlock reportBlock in receiversContext.ReceiversReport)
                            {
                                consoleWriter.WriteLine("\t Receiver  : " + reportBlock.SendersSynchronizationSourceIdentifier);
                                consoleWriter.WriteLine("\t CumulativePacketsLost : " + reportBlock.CumulativePacketsLost);
                            }
                        }
                    }//Disposes the receiver
                }//Disposes the sender
                consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + "Exit");
            }
        }        

        static void TestRtpDumpReader(string path, Media.RtpTools.FileFormat? knownFormat = null)
        {
            //Always use an unknown format for the reader allows each item to be formatted differently
            using (Media.RtpTools.RtpDump.DumpReader reader = new Media.RtpTools.RtpDump.DumpReader(path, knownFormat))
            {

                Console.WriteLine(string.Format(TestingFormat, "Successfully Opened", path));

                while (reader.HasNext)
                {
                    Console.WriteLine(string.Format(TestingFormat, "ReaderPosition", reader.Position));

                    using (Media.RtpTools.RtpToolEntry entry = reader.ReadNext())
                    {

                        Console.WriteLine("EntryToString - " + entry.ToString());

                        //Check for the known format if given, happens for Hex and Binary..
                        //if (knownFormat.HasValue && reader.Format != knownFormat) throw new Exception("RtpDumpReader Format did not match knownFormat");

                        //Show the format of the item found
                        Console.WriteLine(string.Format(TestingFormat, "Format", entry.Format));


                        //Show the IP Address and Source in the entry, Note the entry has a TimevalSize property as well as a ReverseValues property which can change how the values are represented if needed.                        
                        Console.WriteLine(string.Format(TestingFormat, "Source", entry.Source));

                        Console.WriteLine(string.Format(TestingFormat, "Offset", entry.Offset));

                        //Additionally the Blob contains the RD_hdr_t and RD_packet_t but the Data property only will expose the octets required for the packets
                        byte[] data = entry.Data.ToArray();

                        int offset = 0, max = data.Length;

                        if (max == 0) continue;

                        //Determine further action based on the PacketLength, Version etc.
                        if (entry.IsRtcp)
                        {
                            //Attempt to get any packets which correspond to a Media.Rtcp Payload Type which is implemented
                            foreach (Media.Rtcp.RtcpPacket p in Media.Rtcp.RtcpPacket.GetPackets(data, offset, max))
                            {

                                //Use Rtp parsing (Special case in the first set of packets where only the Rtp Header is present with a Version 0 header)
                                if (p.Version != 2) goto PrintRtpOrVatPacketInformation;

                                //Print information about the packet
                                PrintRtcpInformation(p);

                                //Move the offset for the packet
                                offset += p.Length;

                            }//Done with the Media.Rtcp portion

                            //To find another RtpToolEntry
                            continue;
                        }

                        //By convention if there is more data then there should be another RD_hdr_t right here

                        //`This is followed by one binary header (RD_hdr_t) and one RD_packet_t structure for each received packet. `

                        //Obviously this is not the case, and you must determine if there are any more octets which remain to be parsed.

                        //If so then there is only another RD_packet_t structure here describing a rtpPacket?

                    PrintRtpOrVatPacketInformation:
                        //Create a RtpHeaer from the Pointer
                        using (Media.Rtp.RtpHeader header = new Media.Rtp.RtpHeader(data, offset))
                        {
                            if (offset + header.Size < max) //Use the created packet so it can be disposed
                                using (Media.Rtp.RtpPacket p = new Media.Rtp.RtpPacket(header, new Media.Common.MemorySegment(data, offset + Media.Rtp.RtpHeader.Length, max - (offset + Media.Rtp.RtpHeader.Length)), false))
                                {
                                    //Write information about the packet to the console
                                    Console.BackgroundColor = ConsoleColor.Green;
                                    TryPrintPacket(true, p);
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    offset += p.Payload.Count;
                                }

                        }//Done using the header 
                    }//Done using the RtpEntry(entry)
                }//The reader has no more entries
            }//Done using the RtpDumpReader (reader)
        }

        /// <summary>
        /// Creates a <see cref="DumpWriter"/> and writes a single <see cref="Rtp.RtpPacket"/>, and a single <see cref="Rtcp.RtcpPacket"/>.
        /// </summary>
        /// <param name="path">The path to write the packets to</param>
        /// <param name="format">The format the packets should be written in</param>
        static void TestRtpDumpWriter(string path, Media.RtpTools.FileFormat format)
        {
            System.Net.IPEndPoint testingEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7);

            //Use a write to write a RtpPacket
            using (Media.RtpTools.RtpDump.DumpWriter dumpWriter = new Media.RtpTools.RtpDump.DumpWriter(path, format, testingEndPoint))
            {
                //Create a RtpPacket and
                using (var rtpPacket = new Media.Rtp.RtpPacket(new Media.Rtp.RtpHeader(2, true, true, true, 7, 7, 7, 7, 7), new byte[0x01]))
                {
                    //Write it
                    dumpWriter.WritePacket(rtpPacket);
                }

                //Create a  Media.RtcpPacket and
                using (var rtcpPacket = new Media.Rtcp.RtcpPacket(new Media.Rtcp.RtcpHeader(2, 207, true, 7, 7, 7), new byte[0x01]))
                {
                    //Write it
                    dumpWriter.WritePacket(rtcpPacket);
                }


                //----Write some more examples

                //Senders Report
                using (Media.Rtcp.RtcpPacket packet = new Media.Rtcp.RtcpPacket(new byte[] { 0x80, 0xc8, 0x00, 0x06, 0x43, 0x4a, 0x5f, 0x93, 0xd4, 0x92, 0xce, 0xd4, 0x2c, 0x49, 0xba, 0x5e, 0xc4, 0xd0, 0x9f, 0xf4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0))
                {
                    //Write it
                    dumpWriter.WritePacket(packet);
                }


                //Recievers Report and Source Description
                using (Media.Rtcp.RtcpPacket packet = new Media.Rtcp.RtcpPacket
                    (new byte[] { 
                                   //RR
                                   0x81,0xc9,0x00,0x07,
                                   0x69,0xf2,0x79,0x50,
                                   0x61,0x37,0x94,0x50,
                                   0xff,0xff,0xff,0xff,
                                   0x00,0x01,0x00,0x52,
                                   0x00,0x00,0x0e,0xbb,
                                   0xce,0xd4,0xc8,0xf5,
                                   0x00,0x00,0x84,0x28,
                                   //SDES
                                   0x81,0xca,0x00,0x04,
                                   0x69,0xf2,0x79,0x50,
                                   0x01,0x06,0x4a,0x61,
                                   0x79,0x2d,0x50,0x43,
                                   0x00,0x00,0x00,0x00
                                }, 0))
                {//Write it                    
                    dumpWriter.WritePacket(packet);
                }
            }//Done writing packets with the writer
        }

        static void TestRtpTools()
        {
            string currentPath = System.IO.Path.GetDirectoryName(executingAssemblyLocation);

            Console.WriteLine("RtpDump Test - " + currentPath);

            //Delete previous output

            System.IO.File.Delete(currentPath + @"\mybark.rtp");

            System.IO.File.Delete(currentPath + @"\BinaryDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\AsciiDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\HexDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\Header.rtpdump");

            System.IO.File.Delete(currentPath + @"\ShortDump.rtpdump");

            #region Test Reader with Unknown format on example file with expected format

            string file = currentPath + @"\Media\bark.rtp";

            //Should find Media.RtpTools.FileFormat.Binary
            if (System.IO.File.Exists(file)) TestRtpDumpReader(file, Media.RtpTools.FileFormat.Binary);

            if (System.IO.File.Exists(file = currentPath + @"\Media\video.rtpdump")) TestRtpDumpReader(file, Media.RtpTools.FileFormat.Binary);

            #endregion

            #region Test Writer on various formats

            TestRtpDumpWriter(file = currentPath + @"\BinaryDump.rtpdump", Media.RtpTools.FileFormat.Binary);

            TestRtpDumpWriter(file = currentPath + @"\Header.rtpdump", Media.RtpTools.FileFormat.Header);

            TestRtpDumpWriter(file = currentPath + @"\AsciiDump.rtpdump", Media.RtpTools.FileFormat.Ascii);

            TestRtpDumpWriter(file = currentPath + @"\HexDump.rtpdump", Media.RtpTools.FileFormat.Hex);

            TestRtpDumpWriter(file = currentPath + @"\ShortDump.rtpdump", Media.RtpTools.FileFormat.Short);

            #endregion

            #region Test Reader on those expected formats

            TestRtpDumpReader(file = currentPath + @"\BinaryDump.rtpdump", Media.RtpTools.FileFormat.Binary);

            TestRtpDumpReader(file = currentPath + @"\Header.rtpdump", Media.RtpTools.FileFormat.Header);

            TestRtpDumpReader(file = currentPath + @"\AsciiDump.rtpdump", Media.RtpTools.FileFormat.Ascii);

            TestRtpDumpReader(file = currentPath + @"\HexDump.rtpdump", Media.RtpTools.FileFormat.Hex);

            TestRtpDumpReader(file = currentPath + @"\ShortDump.rtpdump", Media.RtpTools.FileFormat.Short);

            #endregion

            #region Read Example rtpdump file 'bark.rtp' and Write the same file as 'mybark.rtp'

            //Maintain a count of how many packets were written for next test
            int writeCount;

            using (Media.RtpTools.RtpDump.DumpReader reader = new Media.RtpTools.RtpDump.DumpReader(currentPath + @"\Media\bark.rtp"))
            {
                //Each item will be returned as a byte[] reguardless of format
                //reader.ReadBinaryFileHeader();

                //Write a file with the same attributes as the example file, needs to use DateTimeOffset
                using (Media.RtpTools.RtpDump.DumpWriter writer = new Media.RtpTools.RtpDump.DumpWriter(currentPath + @"\mybark.rtp", reader.Format, new System.Net.IPEndPoint(0, 7))) //should have reader.Source
                {

                    Console.WriteLine("Successfully opened bark.rtpdump");

                    while (reader.HasNext)
                    {
                        Console.WriteLine(string.Format(TestingFormat, "ReaderPosition", reader.Position));

                        using (Media.RtpTools.RtpToolEntry entry = reader.ReadNext())
                        {

                            //Show the format of the item found
                            Console.WriteLine(string.Format(TestingFormat, "Format", entry.Format));

                            //Show the string representation of the entry
                            Console.WriteLine(string.Format(TestingFormat, "Entry", entry.ToString()));

                            byte[] data = entry.Data.ToArray();

                            int size = data.Length;

                            //Check for Media.RtcpPackets first
                            if (entry.PacketLength == 0)
                            {
                                //Reading compound packets out of a single item
                                foreach (Media.Rtcp.RtcpPacket rtcpPacket in Media.Rtcp.RtcpPacket.GetPackets(data, 0, size))
                                {
                                    Console.WriteLine("Found Media.Rtcp Packet: Type=" + rtcpPacket.PayloadType + " , Length=" + rtcpPacket.Length);

                                    //Writing an item for each packet not the compound.

                                    //Might need facilites for writing multiple Media.RtcpPackets as a single packet

                                    //WritePackets(RtcpPacket[])
                                    //WritePackets(RtcPacket[])
                                    //WriteBinary()
                                    writer.WritePacket(rtcpPacket);
                                }
                            }
                            else
                            {
                                Media.Rtp.RtpPacket rtpPacket = new Media.Rtp.RtpPacket(data, 0);
                                Console.WriteLine("Found Rtp Packet: SequenceNum=" + rtpPacket.SequenceNumber + " , Timestamp=" + rtpPacket.Timestamp + (rtpPacket.Marker ? " MARKER" : string.Empty));
                                writer.WritePacket(rtpPacket);
                            }

                        }

                        writeCount = writer.Count;
                    }
                }
            }

            #endregion

            //ToDo a byte by byte compairson on a dump file before modifying

            System.IO.File.Delete(currentPath + @"\mybark.rtp");

            System.IO.File.Delete(currentPath + @"\BinaryDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\AsciiDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\HexDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\Header.rtpdump");

            System.IO.File.Delete(currentPath + @"\ShortDump.rtpdump");
        }

        static void TestRtspClient(string location, System.Net.NetworkCredential cred = null, Media.Rtsp.RtspClient.ClientProtocolType? protocol = null)
        {
            //For display
            int emptyFrames = 0, incompleteFrames = 0, rtspInterleaved = 0, totalFrames = 0;

            //For allowing the test to run in an automated manner or otherwise some output is disabled.
            bool shouldStop = false, packetEvents = false, interleaveEvents = false, repeatTest = false;

            //Check for a location
            if (string.IsNullOrWhiteSpace(location))
            {
                Console.WriteLine("Enter a RTSP URL and press enter (Or enter to quit):");
                location = Console.ReadLine();
            }

            //Write information about the test to the console
            Console.WriteLine("Location = \"" + location + "\" " + (protocol.HasValue ? "Using Rtp Protocol: " + protocol.Value : string.Empty) + "\n Press a key to continue. Press Q to Skip, Press B to set the buffer size.");

            //Define a RtspClient
            Media.Rtsp.RtspClient client = null;

            //If There was a location given

            ConsoleKey inKey;
            
            if ((inKey = Console.ReadKey().Key) == ConsoleKey.Q) return;

            int bufferSize = Media.Rtsp.RtspClient.DefaultBufferSize;

            if (inKey == ConsoleKey.B)
            {
                Console.WriteLine("Type the buffer size and press return:");
                try
                {
                    bufferSize = int.Parse(Console.ReadLine());
                }
                catch(Exception ex)
                {
                    writeError(ex);
                }
            }

            //Using a new Media.RtspClient optionally with a specified buffer size (0 indicates use the MTU if possible)
            using (client = new Media.Rtsp.RtspClient(location, Media.Rtsp.RtspClient.ClientProtocolType.Tcp, bufferSize))
            {
                //Use the credential specified
                if (cred != null) client.Credential = cred;

                //FileInfo to represent the log
                System.IO.FileInfo rtspLog = new System.IO.FileInfo("rtspLog" + DateTime.UtcNow.ToFileTimeUtc() + ".log.txt");

                //Create a log to write the responses to.
                using (Media.Common.Loggers.FileLogger logWriter = new Media.Common.Loggers.FileLogger(rtspLog))
                {
                    //Attach the logger to the client
                    client.Logger = logWriter;

                    //Attach the Rtp logger, should possibly have IsShared on ILogger.

                    using (Media.Common.Loggers.ConsoleLogger consoleLogger = new Media.Common.Loggers.ConsoleLogger())
                    {
                        client.Client.Logger = consoleLogger;

                        //Define a connection eventHandler
                        Media.Rtsp.RtspClient.RtspClientAction connectHandler = null;
                        connectHandler = (sender, args) =>
                        {
                            if (client == null || client.IsDisposed) return;

                            //Increase ReadTimeout here if required
                            //client.SocketReadTimeout 

                            try
                            {
                                Console.WriteLine("\t*****************\nConnected to :" + client.CurrentLocation);
                                Console.WriteLine("\t*****************\nConnectionTime:" + client.ConnectionTime);

                                //If the client is not already playing, and the client hasn't received any messages yet then start playing
                                if (false == client.IsPlaying && client.MessagesReceived == 0)
                                {
                                    Console.WriteLine("\t*****************\nStarting Playback of :" + client.CurrentLocation);

                                    //Try to start listening
                                    client.StartPlaying();

                                    Console.WriteLine("\t*****************\nStartedListening to :" + client.CurrentLocation);
                                }
                            }
                            catch (Exception ex) { writeError(ex); shouldStop = true; }
                        };

                        //Attach it
                        client.OnConnect += connectHandler;

                        //Define an event for RtpPackets Received.
                        Media.Rtp.RtpClient.RtpPacketHandler rtpPacketReceived = (sender, rtpPacket, tc) => TryPrintClientPacket(sender, true, (Media.Common.IPacket)rtpPacket);

                        //Define an even for RtcpPackets Received
                        Media.Rtp.RtpClient.RtcpPacketHandler rtcpPacketReceived = (sender, rtcpPacket, tc) => TryPrintClientPacket(sender, true, (Media.Common.IPacket)rtcpPacket);

                        //Define an even for RtcpPackets sent
                        Media.Rtp.RtpClient.RtcpPacketHandler rtcpPacketSent = (sender, rtcpPacket, tc) => TryPrintClientPacket(sender, false, (Media.Common.IPacket)rtcpPacket);

                        //Define an even for RtpPackets sent
                        Media.Rtp.RtpClient.RtcpPacketHandler rtpPacketSent = (sender, rtpPacket, tc) => TryPrintClientPacket(sender, false, (Media.Common.IPacket)rtpPacket);

                        //Keep tracking of frames with missing packets.
                        HashSet<Media.Rtp.RtpFrame> missing = new HashSet<Media.Rtp.RtpFrame>();

                        //Define an event for Rtp Frames Changed.
                        Media.Rtp.RtpClient.RtpFrameHandler rtpFrameReceived = (sender, rtpFrame, tc, final) =>
                        {
                            if (rtpFrame.IsDisposed) return;
                            if (rtpFrame.IsEmpty)
                            {
                                ++emptyFrames;
                                Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("\t*******Got a EMTPTY RTP FRAME*******"); Console.BackgroundColor = ConsoleColor.Black;
                            }
                            else if (rtpFrame.IsMissingPackets)
                            {
                                ++incompleteFrames;
                                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\t*******Got a RTPFrame With Missing Packets PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.IsComplete + " HighestSequenceNumber = " + rtpFrame.HighestSequenceNumber); Console.BackgroundColor = ConsoleColor.Black;
                                missing.Add(rtpFrame);
                            }
                            else
                            {
                                ++totalFrames;
                                Console.ForegroundColor = ConsoleColor.Blue; Console.WriteLine("\tGot a RTPFrame(" + rtpFrame.PayloadTypeByte + ") PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.IsComplete + " HighestSequenceNumber = " + rtpFrame.HighestSequenceNumber); Console.BackgroundColor = ConsoleColor.Black;
                            }

                            //A RtpFrame may be changed many times by a RtpClient
                            //If a frame is now complete and it was thought to be missing packets before
                            if (rtpFrame.IsComplete && missing.Remove(rtpFrame))
                            {
                                //Correct the count
                                --incompleteFrames;
                            }

                        };

                        //Define an event to handle 'InterleavedData'
                        //This is usually not required.
                        //You can use this event for large or incomplete packet data or othewise as required from the RtspClient.
                        //Under Rtp Transport this event is used propegate data which does not belong to Rtp from the RtpClient to the RtspClient.
                        Media.Rtp.RtpClient.InterleaveHandler rtpInterleave = (sender, data, offset, count) =>
                        {
                            ++rtspInterleaved;
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("\tInterleaved=>" + count + " Bytes");
                            Console.WriteLine("\tInterleaved=>" + Encoding.ASCII.GetString(data, offset, count));
                            Console.ForegroundColor = ConsoleColor.DarkGray;

                            //If analysing tcp re-transmissions ensure this data can be traced back to whatever packet monitor your using.
                            //if (data[offset] == 36 || !char.IsLetter((char)data[offset]))
                            //{
                            //    System.IO.File.WriteAllBytes(DateTime.UtcNow.ToFileTime() + ".bin", data.Skip(offset).Take(count).ToArray());
                            //}
                        };

                        //Define an event to handle Rtsp Request events
                        client.OnRequest += (sender, message) =>
                        {
                            if (message != null)
                            {
                                string output = "Client Sent " + message.MessageType + " :" + message.ToString();

                                logWriter.Log(output);

                                Console.ForegroundColor = ConsoleColor.DarkCyan;

                                Console.WriteLine(output);

                                Console.ForegroundColor = ConsoleColor.DarkGray;
                            }
                            else
                            {

                                string output = "Null Response";

                                logWriter.Log(output);

                                Console.ForegroundColor = ConsoleColor.Red;

                                Console.WriteLine(output);

                                Console.ForegroundColor = ConsoleColor.DarkGray;
                            }
                        };

                        //Define an event to handle Disconnection from the RtspClient.
                        client.OnDisconnect += (sender, args) => Console.WriteLine("\t*****************Disconnected from :" + client.CurrentLocation);

                        //Define an event to handle Rtsp Response events
                        //Note that this event is also used to handle `pushed` responses which the server sent to the RtspClient without a request.
                        //This can be determined when request is null OR response.MessageType == Request
                        client.OnResponse += (sender, request, response) =>
                        {
                            //Track null and unknown responses
                            if (response != null)
                            {
                                string output = "Client Received " + response.MessageType + " :" + response.ToString();

                                logWriter.Log(output);

                                Console.ForegroundColor = ConsoleColor.DarkGreen;

                                Console.WriteLine(output);

                                Console.ForegroundColor = ConsoleColor.DarkGray;
                            }
                            else
                            {
                                string output = "Null Response";

                                if (request != null)
                                {
                                    if (request.MessageType == Media.Rtsp.RtspMessageType.Request)
                                        output = "Client Received Server Sent " + request.MessageType + " :" + request.ToString();
                                    else
                                        output = "Client Received " + request.MessageType + " :" + request.ToString();
                                }

                                logWriter.Log(output);

                                Console.ForegroundColor = ConsoleColor.Red;

                                Console.WriteLine(output);

                                Console.ForegroundColor = ConsoleColor.DarkGray;
                            }
                        };

                        //Define an event to handle what happens when a Media is played.
                        //args are null if the event applies to all all playing Media.
                        client.OnPlay += (sender, args) =>
                        {
                            //There is a single intentional duality in the design of the pattern utilized for the RtpClient such that                    
                            //client.Client.MaximumRtcpBandwidthPercentage = 25;
                            ///It SHOULD also subsequently limit the maximum amount of CPU the client will be able to use

                            if (args != null)
                            {
                                Console.WriteLine("\t*****************Playing `" + args.ToString() + "`");

                                return;
                            }

                            //If there is no sdp we have not attached events yet
                            if (false == System.IO.File.Exists("current.sdp"))
                            {
                                //Write the sdp that we are playing
                                System.IO.File.WriteAllText("current.sdp", client.SessionDescription.ToString());
                            }



                            //Indicate if LivePlay
                            if (client.LivePlay)
                            {
                                Console.WriteLine("\t*****************Playing from Live Source");
                            }
                            else
                            {
                                //Indicate if StartTime is found
                                if (client.StartTime.HasValue)
                                {
                                    Console.WriteLine("\t*****************Media Start Time:" + client.StartTime);

                                }

                                //Indicate if EndTime is found
                                if (client.EndTime.HasValue)
                                {
                                    Console.WriteLine("\t*****************Media End Time:" + client.EndTime);
                                }
                            }

                            //Show context information
                            foreach (Media.Rtp.RtpClient.TransportContext tc in client.Client.GetTransportContexts())
                            {
                                Console.WriteLine("\t*****************Local Id " + tc.SynchronizationSourceIdentifier + "\t*****************Remote Id " + tc.RemoteSynchronizationSourceIdentifier);
                            }

                        };

                        //Define an event to handle what happens when a Media is paused.
                        //args are null if the event applies to all all playing Media.
                        client.OnPause += (sender, args) =>
                        {
                            if (args != null) Console.WriteLine("\t*****************Pausing Playback `" + args.ToString() + "`(Press Q To Exit)");
                            else Console.WriteLine("\t*****************Pausing All Playback. (Press Q To Exit)");
                        };

                        //Define an event to handle what happens when a Media is stopped.
                        //args are null if the event applies to all all playing Media.
                        client.OnStop += (sender, args) =>
                        {
                            if (args != null) Console.WriteLine("\t*****************Stopping Playback of `" + args.ToString() + "`(Press Q To Exit)");
                            else Console.WriteLine("\t*****************Stopping All Playback. (Press Q To Exit)");
                        };

                        //Attach a logger
                        client.Logger = new Media.Common.Loggers.ConsoleLogger();

                    //Enable echoing headers
                    //client.EchoXHeaders = true;

                    //Enable sending blocksize header (sometimes helpful for better bandwidth utilization)
                    //client.SendBlocksize = true;

                    //If UserAgent should be sent.
                    //client.SendUserAgent = true;
                    //client.UserAgent = "LibVLC/2.1.5 (LIVE555 Streaming Media v2014.05.27)";


                Start:

                        //Allow the client to switch protocols if data is not received.
                        client.AllowAlternateTransport = true;

                        //Connect the RtspClient
                        client.Connect();

                        //Indicate waiting and commands the program accepts
                        Console.WriteLine("Waiting for connection. Press Q to exit\r\nPress K to send KeepAlive\r\nPress D to DisconnectSocket\r\nPress C to Connect\r\nPress A to Attach events for packets\r\nPress E to Detach packet events\r\nPress I to Attach Interleaved events\r\nPress U to Detach Interleaved events\r\nPress P to send a Partial GET_PARAMETER\r\nPress L to RemoveSession\r\nPress X to send Wildcard DESCRIBE.");

                        TimeSpan playingfor = TimeSpan.Zero;

                        DateTime lastNotice = DateTime.MinValue;

                        //Wait for a key press of 'Q' once playing
                        while (false == shouldStop)
                        {
                            System.Threading.Thread.Sleep(0);

                            if (client.IsPlaying)
                            {
                                playingfor = (DateTime.UtcNow - (client.StartedPlaying ?? lastNotice));

                                if ((DateTime.UtcNow - lastNotice).TotalSeconds > 1)
                                {
                                    if (client.IsPlaying)
                                    {
                                        Console.WriteLine("Client Playing for :" + playingfor.ToString());
                                    }

                                    if (false == client.LivePlay && client.EndTime.HasValue)
                                    {

                                        var remaining = playingfor.Subtract(client.EndTime.Value).Negate();

                                        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

                                        Console.WriteLine("Remaining Time in media:" + remaining.ToString());
                                    }

                                    lastNotice = DateTime.UtcNow + TimeSpan.FromSeconds(1);

                                    foreach (var session in client.m_Sessions.Values)
                                    {
                                        Console.WriteLine("SessionId:" + session.SessionId);

                                        Console.WriteLine("Timeout:" + session.Timeout);

                                        Console.WriteLine("SessionTimeRemaining:" + session.SessionTimeRemaining);
                                    }

                                }
                            }
                            else if ((DateTime.UtcNow - lastNotice).TotalSeconds > 1)
                            {
                                Console.WriteLine("Client Not Playing");

                                lastNotice = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                            }

                           

                            //Read a key to determine the stop
                            ConsoleKey read = ConsoleKey.NoName;

                            if (Console.KeyAvailable)
                            {
                                try { read = Console.ReadKey(true).Key; }
                                catch (Exception ex)
                                {
                                    writeError(ex);

                                    read = ConsoleKey.Q;
                                }
                            }

                            //If not stopping
                            if (false == (shouldStop = read == ConsoleKey.Q))
                            {
                                switch (read)
                                {
                                    case ConsoleKey.R:
                                        {
                                            Console.WriteLine("Repeating Test:" + (repeatTest = false == repeatTest));

                                            continue;
                                        }
                                    case ConsoleKey.A:
                                        {

                                            if (packetEvents == false)
                                            {

                                                //Attach events
                                                client.Client.RtpPacketReceieved += rtpPacketReceived;
                                                client.Client.RtpFrameChanged += rtpFrameReceived;
                                                client.Client.RtcpPacketReceieved += rtcpPacketReceived;
                                                client.Client.RtcpPacketSent += rtcpPacketSent;


                                                packetEvents = true;

                                                Console.WriteLine("Events Attached.");
                                            }

                                            continue;
                                        }
                                    case ConsoleKey.C:
                                        {
                                            Console.WriteLine("Connecting Client Socket");

                                            client.Connect();

                                            continue;
                                        }
                                    case ConsoleKey.D:
                                        {
                                            Console.WriteLine("Disconnecting Client Socket");

                                            //Use force parameter to force a new socket to be created when connect is called again.

                                            client.DisconnectSocket();

                                            continue;
                                        }
                                    case ConsoleKey.E:
                                        {
                                            //Remove events
                                            client.Client.RtpPacketReceieved -= rtpPacketReceived;
                                            client.Client.RtpFrameChanged -= rtpFrameReceived;
                                            client.Client.RtcpPacketReceieved -= rtcpPacketReceived;
                                            client.Client.RtcpPacketSent -= rtcpPacketSent;


                                            packetEvents = false;

                                            Console.WriteLine("Events Detached.");

                                            continue;
                                        }
                                    case ConsoleKey.I:
                                        {

                                            if (interleaveEvents == false)
                                            {
                                                client.Client.InterleavedData += rtpInterleave;

                                                Console.WriteLine("Attached Interleave Event.");

                                                interleaveEvents = true;
                                            }


                                            continue;
                                        }
                                    case ConsoleKey.U:
                                        {

                                            client.Client.InterleavedData -= rtpInterleave;

                                            Console.WriteLine("Detached Interleave Event.");

                                            interleaveEvents = false;

                                            continue;
                                        }
                                    case ConsoleKey.K:
                                        {
                                            SendKeepAlive(client);

                                            continue;
                                        }
                                    case ConsoleKey.P:
                                        {
                                            SendRandomPartial(client);

                                            continue;
                                        }
                                    case ConsoleKey.L:
                                        {
                                            //Indicate the session is being removed
                                            Console.WriteLine("Removing Session");

                                            //Send the TEARDOWN with a WildcardLocation.
                                            using (client.RemoveSession(null)) ;

                                            break;
                                        }
                                    case ConsoleKey.X:
                                        {
                                            Console.WriteLine("Sending DESCRIBE Wildcard");

                                            using (Media.Rtsp.RtspMessage describe = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request)
                                            {
                                                RtspMethod = Media.Rtsp.RtspMethod.DESCRIBE,
                                                Location = Media.Rtsp.RtspMessage.Wildcard
                                            })
                                            {

                                                describe.SetHeader("Session", client.SessionId);

                                                //Send the real request
                                                using (client.SendRtspMessage(describe)) ;

                                            }
                                            break;
                                        }
                                    case ConsoleKey.W:
                                        {
                                            if(client.Client != null) foreach (var tc in client.Client.GetTransportContexts())
                                            {
                                                if (tc.RtpSocket != null) Console.WriteLine("RtpReceiveBufferSize" + tc.RtpSocket.ReceiveBufferSize);
                                                if (tc.RtcpSocket != null) Console.WriteLine("RtcpReceiveBufferSize" + tc.RtcpSocket.ReceiveBufferSize);
                                            }

                                            break;
                                        }
                                    case ConsoleKey.Q:
                                        {
                                            Console.WriteLine("Quiting.");
                                            break;
                                        }
                                    default:
                                        {
                                            if (read != ConsoleKey.NoName) Console.WriteLine(read + ": Is not a recognized command.");

                                            System.Threading.Thread.Sleep(0);

                                            continue;
                                        }
                                }
                            }
                        }

                        //if the client is connected still
                        if (client.IsConnected)
                        {
                            //Try to send some requests if quit early before the Teardown.
                            try
                            {

                                int messagesRecievedPrior = client.MessagesReceived;

                                Media.Rtsp.RtspMessage one = null, two = null;

                                //Send a few requests just because
                                if (client.SupportedMethods.Contains(Media.Rtsp.RtspMethod.GET_PARAMETER.ToString()))
                                    one = client.SendGetParameter();
                                else one = client.SendOptions(true);

                                if (one != null) Console.WriteLine(one);

                                //Try to send an options request now, if that fails just send a tear down
                                try { two = client.SendOptions(true); }
                                catch { two = null; }

                                if (two != null) Console.WriteLine(two);

                                //All done with the client
                                client.StopPlaying();

                                if (client.MessagesReceived == messagesRecievedPrior) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(client, "Sending In Play Failed");//Must get a response to at least one of these
                                else Console.WriteLine("Sending Requests In Play Success");
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                                Console.ForegroundColor = ConsoleColor.Red;

                                while (Console.KeyAvailable) Console.ReadKey(true);

                            }
                        }

                        //Output test info before ending
                        if (client.Client != null)
                        {

                            //Print out some information about our program
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.WriteLine("RTCP Info ".PadRight(Console.WindowWidth / 4, '▓'));
                            Console.WriteLine("RtcpBytes Sent: " + client.Client.TotalRtcpBytesSent);
                            Console.WriteLine("Rtcp Packets Sent: " + client.Client.TotalRtcpPacketsSent);
                            Console.WriteLine("RtcpBytes Recieved: " + client.Client.TotalRtcpBytesReceieved);
                            Console.WriteLine("Rtcp Packets Recieved: " + client.Client.TotalRtcpPacketsReceieved);
                            Console.BackgroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("RTP Info".PadRight(Console.WindowWidth / 4, '▓'));
                            Console.WriteLine("Rtp Packets Recieved: " + client.Client.TotalRtpPacketsReceieved);
                            Console.WriteLine("Encountered Frames with missing packets: " + incompleteFrames);
                            Console.WriteLine("Encountered Empty Frames: " + emptyFrames);
                            Console.WriteLine("Total Frames: " + totalFrames);
                            Console.WriteLine("Frames still missing packets: " + missing.Count);
                            Console.BackgroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("RTSP Info".PadRight(Console.WindowWidth / 4, '▓'));
                            Console.WriteLine("Rtsp Requests Sent: " + client.MessagesSent);
                            Console.WriteLine("Rtsp Requests Pushed: " + client.MessagesPushed);
                            Console.WriteLine("Rtsp Requests Retransmitted: " + client.RetransmittedMessages);
                            Console.WriteLine("Rtsp Responses Receieved: " + client.MessagesReceived);
                            Console.WriteLine("Rtsp Missing : " + (client.MessagesSent - client.MessagesReceived));
                            Console.WriteLine("Rtsp Last Message Round Trip Time : " + client.LastMessageRoundTripTime);
                            Console.WriteLine("Rtsp Last Server Delay : " + client.LastServerDelay);
                            Console.WriteLine("Rtsp Interleaved: " + rtspInterleaved);

                            if (System.IO.File.Exists("current.sdp"))
                            {
                                System.IO.File.Delete("current.sdp");
                            }

                        }

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.BackgroundColor = ConsoleColor.Black;

                        if (repeatTest)
                        {
                            shouldStop = false;

                            goto Start;
                        }
                    }
                }
            }
        }        

        /// <summary>
        /// Tests the Media.RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        static void TestServer()
        {
            //Setup a Media.RtspServer on port 554
            using (Media.Rtsp.RtspServer server = new Media.Rtsp.RtspServer(System.Net.IPAddress.Any, Media.Rtsp.RtspMessage.ReliableTransportDefaultPort)
            {
                //new Media.Rtsp.Server.RtspServerDebuggingLogger() 
                Logger = new Media.Rtsp.Server.RtspServerConsoleLogger()
            })
            {
                //Should be working also, allows rtsp requests to be handled over UDP port 555 by default
                //server.EnableUdp();

                //The server will take in Media.RtspSourceStreams and make them available locally

                //Media.Rtsp.Server.MediaTypes.RtspSource source = new Media.Rtsp.Server.MediaTypes.RtspSource("Alpha", "rtsp://quicktime.uvm.edu:1554/waw/wdi05hs2b.mov", Media.Rtsp.RtspClient.ClientProtocolType.Tcp)
                //{
                //    //Will force VLC et al to connect over TCP
                //    //                m_ForceTCP = true
                //};

                //server.AddCredential(source, new System.Net.NetworkCredential("test", "test"), "Basic");

                //If the stream had a username and password
                //source.Client.Credential = new System.Net.NetworkCredential("user", "password");

                //Add the stream to the server
                //server.TryAddMedia(source);

                server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Gamma", "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp"));

                server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("YouTube", "rtsp://v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/video.3gp"));

                server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Delta", "rtsp://46.249.213.93/broadcast/gamerushtv-tablet.3gp"));

                server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Omega", "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"));

                //thaibienbac Test Cameras - Thanks!

                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic", "rtsp://118.70.125.33/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp)); // h264, 1920x1080, 30 fps
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic1", "rtsp://118.70.125.33:20554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic2", "rtsp://118.70.125.33:21554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic3", "rtsp://118.70.125.33:22554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic4", "rtsp://118.70.125.33:23554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic5", "rtsp://118.70.125.33:24554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic6", "rtsp://118.70.125.33:25554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Panasonic7", "rtsp://118.70.125.33:26554/mediainput/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("oem1", "rtsp://admin:12345@118.70.125.33:12554/cam/realmonitor?channel=1&subtype=0", Media.Rtsp.RtspClient.ClientProtocolType.Tcp)); // h264, 1280x960, 30 fps
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("oem2", "rtsp://admin:12345@118.70.125.33:13554/cam/realmonitor?channel=1&subtype=0", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("benco1", "rtsp://118.70.125.33:35554/user=admin&password=&channel=1&stream=0.sdp?real_stream", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("benco2", "rtsp://118.70.125.33:36554/user=admin&password=&channel=1&stream=0.sdp?real_stream", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("benco3", "rtsp://118.70.125.33:37554/user=admin&password=&channel=1&stream=0.sdp?real_stream", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("benco4", "rtsp://118.70.125.33:38554/user=admin&password=&channel=1&stream=0.sdp?real_stream", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("foscam1", "rtsp://hptvn:hptvn@hptvn-com.dyndns.org:9821/videoMain", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("foscam2", "rtsp://hptvn:hptvn@fe7037.myfoscam.org:9826/videoMain", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("foscam3", "rtsp://hptvn:hptvn@eq6842.myfoscam.org:9831/videoMain", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("avtech1", "rtsp://demo:demo@sieuthivienthong.dyndns.org:8081/live/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("avtech2", "rtsp://admin:admin@avm561.ddns.eagleeyes.tw:161/live/h264", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("lilin", "rtsp://admin:pass@118.70.125.33:27554/rtsph2641080p", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("arecont", "rtsp://admin:admin@118.70.125.33:28554/h264.sdp?res=full", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Hikvision", "rtsp://1:1@118.70.181.233:2134/PSIA/Streamingchannels/0", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Hikvision1", "rtsp://1:1@118.70.181.233:2114/PSIA/Streamingchannels/0", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));                
                //server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RtspSource("Keeper", "rtsp://admin:admin@camerakeeper.dyndns.tv/av0_0", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));

                string localPath = System.IO.Path.GetDirectoryName(executingAssemblyLocation);

                //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/PicsTcp through Tcp
                server.TryAddMedia(new Media.Rtsp.Server.MediaTypes.RFC2435Media("PicsTcp", localPath + "\\Media\\JpegTest\\") { Loop = true, ForceTCP = true });

                Media.Rtsp.Server.MediaTypes.RFC2435Media sampleStream = null;// new Media.Rtsp.Server.Streams.RFC2435Stream("SamplePictures", @"C:\Users\Public\Pictures\Sample Pictures\") { Loop = true };

                //Expose Bandit's Pictures through Udp and Tcp
                server.TryAddMedia(sampleStream = new Media.Rtsp.Server.MediaTypes.RFC2435Media("Bandit", localPath + "\\Media\\Bandit\\") { Loop = true });

                //Test Experimental H.264 Encoding
                //server.AddMedia(new Media.Rtsp.Server.Media.RFC6184Media(128, 96, "h264", localPath + "\\Media\\JpegTest\\") { Loop = true });

                //Test Experimental MPEG Encoding
                //server.AddMedia(new Media.Rtsp.Server.Media.RFC2250Media(128, 96, "mpeg", localPath + "\\Media\\JpegTest\\") { Loop = true });

                //Test Http Jpeg Transcoding
                //server.AddMedia(new Media.Rtsp.Server.Media.JPEGMedia("HttpTestJpeg", new Uri("http://118.70.125.33:8000/cgi-bin/camera")));
                //server.AddMedia(new Media.Rtsp.Server.Media.MJPEGMedia("HttpTestMJpeg", new Uri("http://extcam-16.se.axis.com/axis-cgi/mjpg/video.cgi?")));

                //Make a 1080p MJPEG Stream
                Media.Rtsp.Server.MediaTypes.RFC2435Media mirror = new Media.Rtsp.Server.MediaTypes.RFC2435Media("Mirror", null, false, 1920, 1080, false);

                //System.Net.HttpListener http = new System.Net.HttpListener();

                //http.Start();

                //http.Prefixes.Add();

                //Add the stream
                server.TryAddMedia(mirror);

                //Make a thread to take screen shots
                System.Threading.Thread taker = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                {
                    //Get a screen
                    var screen = Screen.PrimaryScreen;

                    //Make a bitmap
                    var bmpScreenshot = new System.Drawing.Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                               
                    //Make the graphics
                    var gfxScreenshot = System.Drawing.Graphics.FromImage(bmpScreenshot);

                    //Could also use mirror.State once started..
                    while (server.IsRunning)
                    {
                        try
                        {

                            //see CopyFromScreen, obtains the data on the screen.
                            gfxScreenshot.CopyFromScreen(System.Drawing.Point.Empty,
                                                        System.Drawing.Point.Empty,
                                                        bmpScreenshot.Size,
                                                        System.Drawing.CopyPixelOperation.SourceCopy);

                            //Convert to JPEG and put in packets
                            mirror.Packetize(bmpScreenshot);

                            //REST
                            System.Threading.Thread.Sleep(50);
                        }
                        catch(Exception ex)
                        {
                            server.Logger.LogException(ex);

                            bmpScreenshot.Dispose();

                            gfxScreenshot.Dispose();

                            if (ex is System.Threading.ThreadAbortException)
                            {
                                System.Threading.Thread.ResetAbort();

                                break;
                            }

                            if (mirror.State == Media.Rtsp.Server.SourceMedia.StreamState.Started)
                            {
                                screen = Screen.PrimaryScreen;

                                bmpScreenshot = new System.Drawing.Bitmap(screen.Bounds.Width, screen.Bounds.Height);

                                gfxScreenshot = System.Drawing.Graphics.FromImage(bmpScreenshot);
                            }
                        }
                    }

                    mirror.Stop();

                    bmpScreenshot.Dispose();

                    bmpScreenshot = null;

                    gfxScreenshot.Dispose();

                    gfxScreenshot = null;

                }), 1);

                //Start the server
                server.Start();

                //Wait for the server to start.
                while (false == server.IsRunning) System.Threading.Thread.Sleep(0);

                //Start taking pictures of the desktop and making packets in a seperate thread.
                taker.Start();

                //If you add more streams they will be started when TryAddMedia is called.

                Console.WriteLine("Listening on: " + server.LocalEndPoint);

                Console.WriteLine("Waiting for input...");
                Console.WriteLine("Press 'U' to Enable Udp on Media.RtspServer");
                Console.WriteLine("Press 'H' to Enable Http on Media.RtspServer");
                Console.WriteLine("Press 'T' to Perform Load SubTest on Media.RtspServer");
                Console.WriteLine("Press 'C' to See how many clients are connected.");
                if (sampleStream != null) Console.WriteLine("Press 'F' to See statistics for " + sampleStream.Name);

                while (true)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                    if (keyInfo.Key == ConsoleKey.Q) break;
                    else if (keyInfo.Key == ConsoleKey.H)
                    {
                        Console.WriteLine("Enabling Http");
                        server.EnableHttpTransport();
                    }
                    else if (keyInfo.Key == ConsoleKey.U)
                    {
                        Console.WriteLine("Enabling Udp");
                        server.EnableUnreliableTransport();
                    }
                    else if (keyInfo.Key == ConsoleKey.F)
                    {
                        Console.WriteLine("======= RFC2435 Stream Information =======");
                        Console.WriteLine("Uptime (Seconds) :" + sampleStream.Uptime.TotalSeconds);
                        Console.WriteLine("Frames Per Second :" + sampleStream.FramesPerSecond);
                        Console.WriteLine("==============");
                    }
                    else if (keyInfo.Key == ConsoleKey.T)
                    {
                        Console.WriteLine("Performing Load Test");
                        System.Threading.ThreadPool.QueueUserWorkItem(o =>
                        {
                            SubTestLoad(server);
                            Console.WriteLine("Load Test Completed!!!!!!!!!!");
                        });
                    }
                    else if (keyInfo.Key == ConsoleKey.C)
                    {
                        Console.WriteLine(server.ActiveConnections + " Active Connections");
                    }
                    else if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }

                server.DisableHttpTransport();

                server.DisableUnreliableTransport();

                Console.WriteLine("Server Streamed : " + server.TotalStreamedBytes);

                Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

                Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

                Console.WriteLine("Stopping Server");

                server.Stop();

                Console.WriteLine("Server Stopped");
            }
        }

        /// <summary>
        /// Tests the Rtp and Media.RtspClient in various modes (Against the server)
        /// </summary>
        static void SubTestLoad(Media.Rtsp.RtspServer server)
        {
            //100 times about a GB in total

            //Get the Degrees Of Parallelism (Shuld be based on ProcessorCount)
            int dop = Environment.ProcessorCount;

            if (server != null)
            {
                if (server.HttpEnabled) dop /= 2;
                if (server.UdpEnabled) dop /= 2;
                dop /= 2;//Tcp
            }

            //Test the server
            ParallelEnumerable.Range(1, 100).AsParallel().WithDegreeOfParallelism(dop).ForAll(i =>
            {
                //Create a client
                if (server != null && server.HttpEnabled && i % 2 == 0)
                {
                    //Use Media.Rtsp / Http
                    using (Media.Rtsp.RtspClient httpClient = new Media.Rtsp.RtspClient("http://127.0.0.1/live/PicsTcp"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Http / Media.Rtsp Test");

                            httpClient.StartPlaying();

                            while (httpClient.Client.TotalRtpBytesReceieved <= 1024) { System.Threading.Thread.Sleep(10); }

                            Console.WriteLine("Test passed");

                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rtp / Http Test Failed: " + ex.Message);
                            Console.BackgroundColor = ConsoleColor.Black;
                            return;
                        }
                    }
                }
                else if (server != null && server.UdpEnabled && i % 3 == 0)
                {
                    //Use Media.Rtsp / Udp
                    using (Media.Rtsp.RtspClient udpClient = new Media.Rtsp.RtspClient("rtspu://127.0.0.1/live/PicsTcp"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Udp / Media.Rtsp Test");

                            udpClient.AllowAlternateTransport = true;

                            udpClient.StartPlaying();

                            while (udpClient.Client.TotalRtpBytesReceieved <= 1024) { System.Threading.Thread.Sleep(10); }

                            Console.WriteLine("Test passed");

                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rtp / Udp Test Failed: " + ex.Message);
                            Console.BackgroundColor = ConsoleColor.Black;
                            return;
                        }
                    }
                }
                else
                {

                    string uri = "rtsp://127.0.0.1/live/Mirror";

                    if (server != null)
                    {
                        try
                        {
                            uri = "rtsp://127.0.0.1/live/" + server.MediaStreams.Skip(Utility.Random.Next(0, server.MediaStreams.Count() - 2)).First().Name;
                        }
                        catch(Exception ex)
                        {
                            //writeError(ex);

                            return;
                        }
                    }

                    //Use Media.Rtsp / Tcp
                    using (Media.Rtsp.RtspClient client = new Media.Rtsp.RtspClient(uri, i % 2 == 0 ? Media.Rtsp.RtspClient.ClientProtocolType.Tcp : Media.Rtsp.RtspClient.ClientProtocolType.Udp))
                    {
                        try
                        {
                            Console.WriteLine("Performing Media.Rtsp Test");

                            client.StartPlaying();

                            while (client.Client.TotalRtpBytesReceieved <= 4096 && client.Client.Uptime.TotalSeconds < 120)
                            {
                                //Test that client disconnection under udp is working
                                if (client.RtpProtocol == System.Net.Sockets.ProtocolType.Udp)
                                {
                                    //Deactivate the client socket if it was connected to test that that media session persists
                                    if (client.IsConnected) client.DisconnectSocket();
                                    else if (client.ClientSequenceNumber % 2 == 0) client.SendKeepAliveRequest(null); //Send a keep alive to test connections and session retrival
                                    else if (client.IsConnected) SendRandomPartial(client); //If connected send a partial request
                                    else client.Connect(); //otherwise connect
                                }

                                System.Threading.Thread.Sleep(10);
                            }

                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.WriteLine("Test passed " + client.Client.TotalRtpBytesReceieved + " " + client.RtpProtocol);
                            Console.BackgroundColor = ConsoleColor.Black;

                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rtp / Tcp Test Failed: " + ex.Message);
                            Console.BackgroundColor = ConsoleColor.Black;
                        }
                    }
                }
            });
        }

        //Should be seperate file with more tests with known types, audio and video and also known values in the data e.g. audio unit size and length, video unit size and length.
        static void TestRFC3640AudioFrame()
        {

            /*
             Todo, put in file with class under UnitTest namespace
             * 
             *  These packets should make a file which can be verified byte for byte and be an exact length, the amount of access units should also be known and verified.
             *  (sdkplayable (1))
             *  
                80e1360209296617bda5a14200100fd80140342c2c3a65869107a140a876175c5f0cbe12b2f5592ca9952f7255932e0b450c00003a8b2290a94e863d78cc036cfd5b11a9651761022c52243687c71a25352f86d219365d1f45f29685073b7b6e19409959545a080e12d9cd4852d0df9790ae0c9f7df8abfa46aecf566c450e4a0e7f4f678f7f5cca81e13009db9505a69c291f575b71dff9161a3d9a524556bd5862b4b2701942d3619e7022826d5ab6787e0f8bf2bd8f4ed5af3afcb0e7b155209ef3bdbf171e534586cb0834eac13c26061c3a500a31046952a1f92dea45af63581332103e524166890bdddb8b8db7c2c4440c93ee714946feb94508b4ca49f7c324b3894610102223cf30c21038866d19a9adb5e57c768d3539c121c2198872c63930a2d96f8dcabd0d332096db19f9efbc6a6fb55b2ab762b14e60411d02667e4fafa9660b004266add34743ba77292c6547bad68b612c18d55a2dba714542409e7f544376ea15a01b10a032cb4c287def045412544d0cd35f79e028b7d8d2994f0441252b3873b4f99c7a88f803385fca42d0ce1aed89a907d860763b35848a9513e3daf2af69455669455bf3dc9386dec0df43fd6fd465781ccea894d43af67076acb3608480766338c4542c03b12d45c6f2a7c307307992e30f0946d574f5f1b83983086b8cb663b27979a33ce2c8d1792da272535fd8ef74d13aac590e1319ec7427f59fc1d1de

                80e1360309296a16bda5a14200100f40014234243478c50d83446bdb8e5552cdf06f8a95564a085e4a41fe0b4e01ec903854a1b88b4040dbcc7b755cfddcb3963a21bf0fa841602aa408b010dd8f17fc4fa81c41831c294ad621511120de60d8f2d19faa2ad594e1548ff4a42c3ff1b4a9e412923ccddddf18de8d44e9a6ddf82001912e17e79f2f95ee5c54fe7d464dc4921abbcac933a8113f22a6fb65f1ed2ce7efb469484c1410c855e9eccbb7abdbdb22678d67bba3b4c43ecff36e3ee0ebb69258263deed09f0db8c430d4f2b42bd9aec6e82c8f2b729c8b2ed2c143d8494e5d75e9124b8b7fb3d7bd573a9d024340eca9c53106413323269d39ff9efb620c18bf70381c0304634488208a90284f2998e2600c6918a01a75605ffe6f54391122a081b60054b334992946e1d5ba11a4ef11f2f51e56fc9707535326cb1f0e9179ad3692ee8d17cacea4f7d8ecfebbd23ac1892c43f18f41646994689ae0f93291a9987b3897971f275cab9597f097e970541d79551d54e9db1867274eec76db23072c2d10d58a28463ae50780906a537a9ea5449cda22e2ebcb8a5ebe3c61f33c361e5a0cef8d314a04310d39c40a4c8b9abad6e44170136c6c9560f2b1f306590708809dca7884e055dbdf90675ccc2e09525d859594ae7c0a4c2c937e566fd0ece543a14a6cbe03aa10f770d6d4f239fd75b94a38

                80e1360409296e15bda5a14200100f70013a342910aa441692bd72a85d32d75920a978bcba94ba03420d0002008c2c6c473a7532edfc46c89033860d022d145cfdcfe9b98e3de154413022260da76749723ccc3997ce7763ca1ec5b21d664646176464ffbd9c42c158826d9ed322c717501eced94866cbaa437123d8b4e0d79f59b42eab5bf90eb4ad2ef7ba4ce0bcc65da7852079b3dd2b2c6586d4be1ba1721006e72418aa6e5ad35a685c9b309ada17969cd16b0a0d236cb764aa9f1e9c78d8336c219a2d5ee846253d9324143f40da1d7577f80680bd70797c1c3aaa96ebf2264538ed1d7d9757d522944658d04c4170f9f19bc9ee939ad638f1c8966312c3796049e1c69ea96a6f2e9b350d2989012d32d16220a438d9b4582422bf461b090428cb988256e5bdadbe9cac3ebfc35d557293547259c4e3e3ff1bcfe7aea5d9f306540da4f10f0d58ae346230529e570d28562ce1d328e02b0b1ee7d569ad9604f1f10f030e52c620b65ad5eac958a5afe3e6c02c16fdfdf21180861c4982f028b006ab6fed82875d19b03b9fcae4311188229ab174ee15743269cac40e2eaecb2879f07e0f3f71886eb641608f56010e5498f5e695350876af73ef58080cb2056ac48ae9b661a15235b0a2c744564634f7b28d482146f6fb0aa2e19b04eaf224c8e9d16c154c1e2deba83483e37273cf04508315a290e2dcb01301c1
             */

            //The data contained in x Packets
            byte[][] packetBytes = new byte[][]
                {
                    //Packet 1
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c48d49d88519b64c592f00100fc801363428f0e62416830944b24a812a5355525454cb84280393acf51e5e0c076d80de6f6f8b6585d71b6790b9414a5577a7dc7e90d59e8161a3dba1d693a06dbb6f270ee682a63115bd8b5dae5d5061db46b889b5155dac5a71aba99ed775778faa677b5f64f49d5ea2aa9bf505ac932621723f779ede5a4aa295f0e5f6d866420ee1052e64d3d42917cab0bfe055f68449110152a6134188f5680054adf0c7d484317e7fa9a7ffb209b7257f905dbcfcdd412744b6e8bfa148aec7054f7a3bd319e2b3bd095196664f5ef5bdd069484c8bfe214faaa71155e56fb337cc497820cb2428497e22418f9c914b4a5c4628b7ab427452aa0e4cbe3254e12fb3f3532ee1cc8cc20cb9b33b6b6668d1cb0704d31a5b38665cc35346da4ae84f54a2922952a85cd06304ba0bc4af7a6c370cb6b12316a3acc0ab0f24a49b955726ca953473e891dce2c5983293a38b1b28896c57010211a17ed3791b57c5b434ed0ad05cdaba3dacbb581d2e5d4e9a6dc508a9ca2df88cd8cc7e284a62d2e27e84314459ed7852ea2a6bd03005ac0dded6a1fd08e623bae1274693126552a46919d954be6dd5d5543fbb75e574b57bf7845676525235d1a7910fde93967db4eaee3d1f85671d9c696e6086eded6d1f56b74d6dc63c8c6639d908ef120000000000000000000000000000000000000000000000000000000000000000f4"),
                    //Packet 2
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c48e49d88918b64c592f00100fc80132342a30da0c24484914854956254aa99a400113a419efd06aee69faf6c782128adbe064ae373dc53492edbf586c6db29caf956dd881d5a4b063f684c53d92cb301641c348cf42da0d9bc8a38e92454c82c787b867ab99b816b518f7f06d75901be21f57d1a560885541484cfb77cf30a6952e65fb032bd04aab7718cd132f202080ef137e7d46c009e0ed5e43896f2080a6249a2ce04fbef8506002e5a054bbf693ca4ea594404432d94f2110c74d203e0be316f151e790c00a9d2a6ca499176b24016db62467ae55d3a74c1ad3991af4a6290b0badf52f02f7be58e44265a4a4cf872f79a87d7163cd8b47f5c5ba24f84254df46fd7f24700679cfa104f15c1b4ec508cf95c4f5a92c46ebbd6ac1780a278a7e9c4fae2d2ac86857511ef9f47b3d6cb737b5aeaed25f96fc16c968b16d4125f15089374bde88399276e4464751db44a115e84b952f43aabaa45451a65d2ac6988d4bba7a33b3aa6a2b22daf8cb04565be6775b39bd2ba5b48a590b1a99624052a79dc51992edd77e86a286dd746e22714166dd04b92aabede355791391b9b646a3a387b65d6e6dd697289875195735bbf375188c5b353b3645c8954e93fc5a178716d62557fdbf87ccb8b0e862d44dd6e9beee6ef6f4fad9700a76d3322192a49d95aa6d23bc3800000000000000000000000000000000000000000000000000000003ae"),
                    ////Packet 3
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c48f49d88d18b64c592f00100fc801345429315a0c2282997658a9012544a0cbba254098f396fe440269958e2709aee22965e6bea4f85d4f012799befd0b786ad726b6f761ef8e4736aa0fbf56b126ec52df9f5b33bdaba6abb7aa565f7c48ca7a655d4da6db1b088da2e6596aad2b10560fb9b5a3e3a3c8936a0c6a96712a51b8b76e7e82c2a288d9c20b9d8c4edd22d3a6e59a3ecc988f2ae99fa37b5f0c0fb31ef4b0199962b9b5044b78a9dda382971367262311756d265906cb2e3251d6a7e8c2810768861639a96299896f5b682c8305d564dc7920112c573d66346922c31569ca7e48af20c62d5622c21e40eb4aa60998f42b5b0b10c28c486e69a5a007e867e5a61a9a89a2a3dc2d09ea77cee4c13432f20ba741822aa0c598c68c6bed5215ba28beda33df2249314a605568c05e33093582aed5e654335e2cd73bcda55e5e626de3f13ed4a8970558ed23afb7cc1d66f580e6b916198295e2f32e138815414ae7fea2a4a301b639555893efbe948883df2e88729cf51272a5bd4e94cc8b6263adf93b78e60415d5f14ec5915de49d25f2c467b43a88b6a57df3a54e0df2be895a01cc877ed1bd6bdb6c50dd1148898d230f8cbedf41bbd28d3f92aa2c8df31a65f808737449e9b8a4b5392bb6b2cb6083c0f439d2face45f81b3bcd844d4c84c67788800000000000000000000000000000000000000000000000000000000000007d"),
                    //Packet 4
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c49049d89118b64c592f0010101801209fb2b98ac60d711cd68e6cd74ca9d9571aa8aaaa5abc71a6ab714ab57297755542ceeeee536a55cd8eb7e53ed9eebd1e753e2be4f2be6fadfd8abec43ed78dcab0fa0691c8b314dc4616b1f2bd1d9c676ae5eeb7e78c2e167ab36c5455acf06dd2a0906f751d85baf9732a9441720d0aa17c536557de41f686b49e44494640c528edcca9f4a995118d2db469ecc1a5798202abc1d947ab29cd169a9f2411ea992ea9e6921eef5a42a05d8cfe80a37345f2c83bea934a926cec4122c1e41a25e3b81647b4994c25d614714e8288b7af2363d373cedf2a975ef92e8c67af782dcfbc739ceea3b7ea9b61dec6cbbbe3ba3fedeb53f9d772177ccc30b61e94ea4167471d877aa58d4b76436ef5641c86aee6bd6d03c4e524c27773f42731ca3331b8e397636801b0d9429095f5be4e82254298ce935b89bf7f5159122b0dc3bc05dfca932eea395356d4c1b11902de2d0b162580290ac4bab661a04308a49a120b8894878d4a95bc4a805969d2aab665f151a7ed39aa567cd0f1d0ebcbc5d1f52300be55bad17b9fb5edfc6efbc4718b46d9e6310e8d1b06262dc9c250126ed2844a3b3fb7d3a13189a40a35a3ec4c789a83646c361b45c9e2baaf8be2f89c95f48eb0f6da3e3be0dad8bdea7bcfb5b5520c700c6746af1b5b22c75108d529689d79da567a95ee3af973834bca2dd00214e9caac1c64a80c362ac071b521749702f49f"),
                    //Packet 5
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c49149d89518b64c592f00100f78013af420566a4b161ac2a1a7aea6f557b4d5e4b950644941245658186df97e73193658dc1f3b6ece7b9fc1439380e1546aac29ade32d404ecd63babdf793055eba15c506d4166c8755b4730e21f392e0ec70aa6a758b3c60d6841a245b90f01e47c9ea11cfafcb6f9c23407d61eb453ba5bedc8cba50a99ae9aac77fe0f1f9a5879eaecc8f0cdb81e24eae192422f2d9480b8c9163934b77afb3f582328b4b15467142d15f1926a9aa2d53fce3b2ea57ae90de5208609a5f0ec2b21c09a0035321b1ecdbc7c493bb91b789bb4936b1e35f418a3af8135273a128077a83cdedee199f329c8bf6093153225975579f60aa44d7458ceab7a9d441978c1cb82ceab533aa6b53824b64544d04c7a675813528b3797b8dc66f6d323554fd94db622b8f6d688767eb773a2a31f226c721fac4ab33d3fd3c7e2b38e85def11b03cdd9edcba6d99b276ed2618574d683c4b44a4424f4722a82fdc3040e255d28605b6da356fcbd731cb45fe6cabf0a345140460c34932bd76f4ce55ea6bf2ae6563a4dbd196dcfaba8230e959a48c0c2070eae098172e63e138b6f60b44fab82ba19e34583e3f314e4e1a71459f83a31d206cf09ea9d60c41e1ecedea18cbc3dd8ecf272590b055e83309004d9aae7289c8ca1c5d8eec804cab040aa33adab3120a26b76e6e58cc9b2a6c6b3800000000000007"),
                    //Packet 6
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c49249d89918b64c592f0010118001363420563a63328503b32b1132d797749152520289775ce741705a7c38802af9b99e06e5a60b2983c6ec348d87c9ca333040e731a67dde7f19d6bae3ce778bbd780d5e9940baae9f677784dca48e63a77f5ebca0e174565f5eef1491448571e11d1deeaf0cca718d5e0830a124180498a7f372df8281b2a94e1a919a5dcc7850de1c48005fdb086f1cc72e34bcd35f35352e1cba4a492d4e2538031ce71712bd498da5243db3cf6ec6a69ebc52452869a29cec19b39d66f036db353471e6d260f8ebea89e655e984958508b9927bee4f970b222c6c2bfa9c1c9fef5219466c2905adc38699c126dc244a1a26c6905102ba522b0c15eb98460afe6c96322801bce6a25bd86d371171dba75f42a1b6e9829dd761e46443bbc7e1c9a1d84a15a546f47be3a7bd070b6f66440d2c7149c7d78225a3a529c516bb0d140a78aa5855c7447da1d3a1d8e13c79b416f7df44e5bba05f6c70f9deafe7e7371f9fcf964e38e27ab7bab260ec40fbbf17deeb4acaed977c4e1e54f1106ca37ea7c315149c7158ef1d9c7cd5feb1567567b37a7b5e540e70817d5e63ddbd4d3c7218a46123aa745d9c0aa7ae3885dc1b751f86e101740ec74d424fb87aa619c8ed32d0bd1edd56421fdaaa505980b781794fa19903d3de8bf92dfdf33f9b2d1e5f076c4ba0dfaa9d517973c717f8cf40c65bf633cac0f17b14de713303baa8127b0fcfe762f51e88944dbca4d67f63ddba9242a5e3dca84c53d7fe973b87030fc57a778276af6edd0280ee1eaa5d8dcdd4c73e5b3c"),
                    // Skip a few and put another packet :)
                    Media.Common.Extensions.String.StringExtensions.HexStringToBytes("80e1c49849d8b118b64c592f00100ed0013ef42056782b1108c4825060d43159699a82c9512a52aa04915902db57e4b12254d4b85fae649c4b340d9dd5b2f9a5ea3280b038ee5c673151079894a12fce4ae42e05a8c503bedf25641ce2e3a475f762f6472f7d768caaa35c10ddcac5333ea755c6c933914ecb71a3a89f171ac58c6a315adbf01b4a140a0998f3a1aa9a543ae9aed42936c3339b616430bdd6ba7dcbc9f8d6aa0133876fd28b457fe1740d1a3cdb64864082143152ce2cf62444db8b45474ba1574e3a0a9ca509de108263865170ec2aaef68398654b994a2d0f0d5c55eb334593497c0a1adb7a51e489f47553ec6be47ecb3b626519e30ed18ecefa1ab503188f5ae1cab3164c651cfa1a9a1092b6d9c3bbb2c45bfb9d764d249324ad3e3ecb41c3f83d5aa9b944ed81a4a9d98a567eb3bf5ab953950504ebea3b195530bc50527e921384474998cf42f0920d817448c932c752857463462605da154ab9f3cb4e93e4459241925043162d10ad45a3098e44359a4bd2d9262ca48ed0d071f57b218bb6559b1acaec2a7c6b23707bf04b26151d1d1c60aba94fac5c40471e6530566969a3878d7985141c36d5e55d6b8d63994f61ac53cc8a929e5468d143494f1a55251ca8e62a4cc157a3aa3eabd06368739ca49051915273b3d018f472800000000074"),
                };

            //The data from the packets given above will be placed into this file after depacketization
            string outputFileName = "Test.aac";

            //Create the file every time the test starts
            using (var fs = new System.IO.FileStream(outputFileName, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
            {
                //Create managed packets from the binary data
                foreach (byte[] packet in packetBytes)
                {
                    //Create the managed packet from that binary data
                    using (Media.Rtp.RtpPacket aManagedPacket = new Media.Rtp.RtpPacket(packet, 0))
                    {
                        //Create a RtpFrame from the managed packet
                        using (Media.Rtp.RtpFrame managedFrame = new Media.Rtp.RtpFrame(aManagedPacket))
                        {
                            //The rtp profile contains the logic required to `depacketize` the data.
                            //E.g take it from it's RtpPacket form and into something a decoder can utilize
                            using (Media.Rtsp.Server.MediaTypes.RFC3640Media.RFC3640Frame profileFrame = new Media.Rtsp.Server.MediaTypes.RFC3640Media.RFC3640Frame(managedFrame))
                            {
                                //Example of Media Description
                                //a=rtpmap:97 mpeg4-generic/8000/1
                                //a=fmtp:97 streamtype=5; profile-level-id=15; mode=AAC-hbr; config=1588; sizeLength=13; indexLength=3; indexDeltaLength=3; profile=1; bitrate=32000;

                                //According to the data contained in the stream this is how many bytes are contained in the complete access unit which did not appear
                                //In the data being depacketized, while this value is > 0 the file will likely not be playable.
                                int remainingInNextPacket, unitsParsed;

                                //Depacketize the contained data into a buffer on the managed frame instance.
                                profileFrame.Depacketize(out unitsParsed, out remainingInNextPacket,  //Keep track of how many access units were contained and how many bytes which were not present in the access unit which were specified by it's size
                                    true, //This is specified by the profile (SDP)
                                    2, 1, 11,  //These values come from the config = portion (1588)
                                    13, 3, 3, //These values from from the profile
                                    0, 0, 0, false, 0, 0,
                                    //These values are optional
                                    null, //Could be used when a constant auSize is expected, a header could be generated only one time and appended as required
                                    false,  //Do not add padding
                                    false, // do not include the Au Header Section in the buffer
                                    false); // do not include the Aux Data Section in the buffer

                                //Should have X access units

                                Console.WriteLine("Contained Access Units = " + unitsParsed);

                                //The data may require more data from other packets

                                Console.WriteLine("Data remaining in next packet = " + remainingInNextPacket);

                                //Depending on the format of the audio you may required this function to create a header which should precede the data in the buffer when going to a decoder.
                                byte[] header = Media.Rtsp.Server.MediaTypes.RFC3640Media.RFC3640Frame.CreateADTSHeader(2, 11, 1, (int)profileFrame.Buffer.Length);

                                //Write the header
                                fs.Write(header, 0, header.Length);

                                //Write the data in the buffer to the stream directly
                                profileFrame.Buffer.CopyTo(fs);
                            }
                        }
                    }
                }
            }

            if (System.IO.File.Exists(outputFileName)) System.IO.File.Delete(outputFileName);
        }

        /// <summary>
        /// Performs the Unit Tests for the SessionDescriptionProtocol classes
        /// </summary>
        static void TestSdp()
        {
            #region Old Way of `testing`

            //Test parsing a ssp.
            Media.Sdp.SessionDescription sd = new Media.Sdp.SessionDescription(@"v=0
o=jdoe 2890844526 2890842807 IN IP4 10.47.16.5
s=SDP Seminar
i=A Seminar on the session description protocol
u=http://www.example.com/seminars/sdp.pdf
e=j.doe@example.com (Jane Doe)
c=IN IP4 224.2.17.12/127
t=2873397496 2873404696
a=recvonly
m=audio 49170 RTP/AVP 0
m=video 51372 RTP/AVP 99
a=rtpmap:99 h263-1998/90000");

            Console.WriteLine(sd.ToString());

            Media.Sdp.SessionDescriptionLine connectionLine = sd.ConnectionLine;

            if (connectionLine == null) throw new Exception("Cannot find Connection Line");

            //make a new Sdp using the media descriptions from the old but a new name

            sd = new Media.Sdp.SessionDescription(0)
            {
                //OriginatorAndSessionIdentifier = sd.OriginatorAndSessionIdentifier,
                SessionName = sd.SessionName,
                MediaDescriptions = sd.MediaDescriptions,
                TimeDescriptions = sd.TimeDescriptions,

            };

            //Test another one
            sd = new Media.Sdp.SessionDescription("v=0\r\no=StreamingServer 3219006789 1223277283000 IN IP4 10.8.127.4\r\ns=/sample_100kbit.mp4\r\nu=http:///\r\ne=admin@\r\nc=IN IP4 0.0.0.0\r\nb=AS:96\r\nt=0 0\r\na=control:*\r\na=mpeg4-iod:\"data:application/mpeg4-iod;base64,AoJrAE///w/z/wOBdgABQNhkYXRhOmF\"");

            Console.WriteLine(sd);

            /*
             https://tools.ietf.org/html/rfc4975
             * 
               Figure 2: Example MSRP Exchange

   Alice's request begins with the MSRP start line, which contains a
   transaction identifier that is also used for request framing.  Next
   she includes the path of URIs to the destination in the To-Path
   header field, and her own URI in the From-Path header field.  In this
   typical case, there is just one "hop", so there is only one URI in
   each path header field.  She also includes a message ID, which she
   can use to correlate status reports with the original message.  Next
   she puts the actual content.  Finally, she closes the request with an
   end-line of seven hyphens, the transaction identifier, and a "$" to
   indicate that this request contains the end of a complete message.
             * 
             * 
            5.  Key Concepts

5.1.  MSRP Framing and Message Chunking

   Messages sent using MSRP can be very large and can be delivered in
   several SEND requests, where each SEND request contains one chunk of
   the overall message.  Long chunks may be interrupted in mid-
   transmission to ensure fairness across shared transport connections.
   To support this, MSRP uses a boundary-based framing mechanism.  The
   start line of an MSRP request contains a unique identifier that is
   also used to indicate the end of the request.  Included at the end of
   the end-line, there is a flag that indicates whether this is the last
   chunk of data for this message or whether the message will be
   continued in a subsequent chunk.  There is also a Byte-Range header
   field in the request that indicates the overall position of this
   chunk inside the complete message.

   For example, the following snippet of two SEND requests demonstrates
   a message that contains the text "abcdEFGH" being sent as two chunks.

    MSRP dkei38sd SEND
    Message-ID: 4564dpWd
    Byte-Range: 1-* /8
    Content-Type: text/plain

    abcd
    -------dkei38sd+

    MSRP dkei38ia SEND
    Message-ID: 4564dpWd
    Byte-Range: 5-8/8
    Content-Type: text/plain

    EFGH
    -------dkei38ia$

             */

            sd = new Media.Sdp.SessionDescription(@"c=IN IP4 atlanta.example.com
   m=message 7654 TCP/MSRP *
   a=accept-types:text/plain
   a=path:msrp://atlanta.example.com:7654/jshA7weztas;tcp");

            if (sd.MediaDescriptions.First().MediaType != Media.Sdp.MediaType.message
                ||
                sd.MediaDescriptions.First().MediaProtocol != "TCP/MSRP"
                ||
                sd.MediaDescriptions.First().MediaPort != 7654
                ||
                sd.Lines.Count() != 4 ||
                sd.MediaDescriptions.First().MediaFormat != "*") throw new Exception("Did not parse media line correctly");

            if (sd.Length > sd.ToString().Length) throw new Exception("Did not calculate length correctly");

            Console.WriteLine(sd.ToString());

            #endregion

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(SDPUnitTests));
        }

        static void TestContainerImplementations()
        {
            string localPath = System.IO.Path.GetDirectoryName(executingAssemblyLocation);

            //Todo allow reletive paths?

            #region BaseMediaReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/mp4/") || System.IO.Directory.Exists(localPath + "/Media/Video/mov/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/mp4/").Concat(System.IO.Directory.GetFiles(localPath + "/Media/Video/mov/")))
                {
                    using (Media.Containers.BaseMedia.BaseMediaReader reader = new Media.Containers.BaseMedia.BaseMediaReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Box:" + reader.Root.ToString());

                        Console.WriteLine("Boxes:");

                        foreach (var box in reader)
                        {
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + box.Offset);
                            Console.WriteLine("DataOffset: " + box.DataOffset);
                            Console.WriteLine("Complete: " + box.IsComplete);
                            Console.WriteLine("Name: " + box.ToString());
                            Console.WriteLine("DataSize: " + box.DataSize);
                            Console.WriteLine("TotalSize: " + box.TotalSize);
                            Console.WriteLine("ParentBox: " + Media.Containers.BaseMedia.BaseMediaReader.ParentBoxes.Contains(Media.Containers.BaseMedia.BaseMediaReader.ToUTF8FourCharacterCode(box.Identifier)));
                        }


                        Console.WriteLine("File Level Properties");

                        Console.WriteLine("Created:" + reader.Created);

                        Console.WriteLine("Last Modified:" + reader.Modified);

                        Console.WriteLine("Movie Duration:" + reader.Duration);

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

                }


            Console.WriteLine("Url Test");

            var location = new Uri("http://download.blender.org/peach/bigbuckbunny_movies/BigBuckBunny_320x180.mp4");

            var bufferSize = 8192;

            //Using a Download...
            using (var dl = Common.Extensions.Stream.StreamExtensions.HttpWebRequestDownload(location))
            {
                //The constructor waits for the 'Root' node to be populated, this is mostly so that files which are joined have at least the information for the previous file in the stream...
                using (Media.Containers.BaseMedia.BaseMediaReader reader = new Media.Containers.BaseMedia.BaseMediaReader(location, dl, bufferSize))
                {
                    Console.WriteLine("Path:" + reader.Source);

                    Console.WriteLine("Total Size:" + reader.Length);

                    Console.WriteLine("Root Box:" + reader.Root.ToString());

                    Console.WriteLine("Boxes:");

                    //Read each box in the reader
                    foreach (var box in reader)
                    {
                        Console.WriteLine("Position:" + reader.Position);
                        Console.WriteLine("Offset: " + box.Offset);
                        Console.WriteLine("DataOffset: " + box.DataOffset);
                        Console.WriteLine("Complete: " + box.IsComplete);
                        Console.WriteLine("Name: " + box.ToString());
                        Console.WriteLine("DataSize: " + box.DataSize);
                        Console.WriteLine("TotalSize: " + box.TotalSize);
                        Console.WriteLine("ParentBox: " + Media.Containers.BaseMedia.BaseMediaReader.ParentBoxes.Contains(Media.Containers.BaseMedia.BaseMediaReader.ToUTF8FourCharacterCode(box.Identifier)));

                        //Buffer on demand...
                        while (//box.DataSize > reader.Remaining || //box.IsComplete == false
                            reader.Remaining - (reader.Position + box.TotalSize) <= 8 && //Where 8 should be reader.MinimumNodeSize
                            reader.Buffering) 
                        {
                            Console.WriteLine("Buffering...");

                            Console.WriteLine("Length:" + reader.Length);

                            Console.WriteLine("Position:" + reader.Position);

                            //Expose wait handle..

                            System.Threading.Thread.Yield();
                        }
                    }


                    Console.WriteLine("File Level Properties");

                    Console.WriteLine("Created:" + reader.Created);

                    Console.WriteLine("Last Modified:" + reader.Modified);

                    Console.WriteLine("Movie Duration:" + reader.Duration);

                    Console.WriteLine("Track Information:");

                    foreach (var track in reader.GetTracks()) DumpTrack(track);
                }
            }

            //Same thing using WebClient

            using (var dl = Common.Extensions.Stream.StreamExtensions.WebClientDownload(location))
            {
                using (Media.Containers.BaseMedia.BaseMediaReader reader = new Media.Containers.BaseMedia.BaseMediaReader(location, dl, bufferSize))
                {
                    Console.WriteLine("Path:" + reader.Source);

                    Console.WriteLine("Total Size:" + reader.Length);

                    Console.WriteLine("Root Box:" + reader.Root.ToString());

                    Console.WriteLine("Boxes:");

                    //Read each box in the reader
                    foreach (var box in reader)
                    {
                        //Buffer on demand...
                        while (reader.Buffering && reader.Remaining - (reader.Position + box.TotalSize) <= bufferSize)
                        {
                            Console.WriteLine("Buffering...");

                            Console.WriteLine("Length:" + reader.Length);

                            Console.WriteLine("Position:" + reader.Position);

                            //Expose wait handle..

                            System.Threading.Thread.Yield();
                        }

                        Console.WriteLine("Position:" + reader.Position);
                        Console.WriteLine("Offset: " + box.Offset);
                        Console.WriteLine("DataOffset: " + box.DataOffset);
                        Console.WriteLine("Complete: " + box.IsComplete);
                        Console.WriteLine("Name: " + box.ToString());
                        Console.WriteLine("DataSize: " + box.DataSize);
                        Console.WriteLine("TotalSize: " + box.TotalSize);
                        Console.WriteLine("ParentBox: " + Media.Containers.BaseMedia.BaseMediaReader.ParentBoxes.Contains(Media.Containers.BaseMedia.BaseMediaReader.ToUTF8FourCharacterCode(box.Identifier)));
                    }


                    Console.WriteLine("File Level Properties");

                    Console.WriteLine("Created:" + reader.Created);

                    Console.WriteLine("Last Modified:" + reader.Modified);

                    Console.WriteLine("Movie Duration:" + reader.Duration);

                    Console.WriteLine("Track Information:");

                    foreach (var track in reader.GetTracks()) DumpTrack(track);
                }
            }            

            #endregion

            #region RiffReader

            Console.WriteLine("Url Test");

            //Really a mp4?
            location = new Uri("http://mirrorblender.top-ix.org/peach/bigbuckbunny_movies/big_buck_bunny_1080p_surround.avi");

            //Using a Download...
            using (var dl = Common.Extensions.Stream.StreamExtensions.HttpWebRequestDownload(location))
            {
                if(dl.Length > 0) using (Media.Containers.Riff.RiffReader reader = new Media.Containers.Riff.RiffReader(location, dl, bufferSize))
                {
                    Console.WriteLine("Path:" + reader.Source);
                    Console.WriteLine("Total Size:" + reader.Length);

                    Console.WriteLine("Root Chunk:" + Media.Containers.Riff.RiffReader.ToFourCharacterCode(reader.Root.Identifier));

                    Console.WriteLine("Chunks:");

                    foreach (var chunk in reader)
                    {
                        //Buffer on demand...
                        while (reader.Buffering && reader.Remaining - chunk.TotalSize <= bufferSize)
                        {
                            Console.WriteLine("Buffering...");

                            Console.WriteLine("Length:" + reader.Length);

                            Console.WriteLine("Position:" + reader.Position);

                            //Expose wait handle..

                            System.Threading.Thread.Yield();
                        }

                        Console.WriteLine("Position:" + reader.Position);
                        Console.WriteLine("Offset: " + chunk.Offset);
                        Console.WriteLine("DataOffset: " + chunk.DataOffset);
                        Console.WriteLine("Complete: " + chunk.IsComplete);

                        string name = Media.Containers.Riff.RiffReader.ToFourCharacterCode(chunk.Identifier);

                        Console.WriteLine("Name: " + name);

                        //Show how the common type can be read.
                        if (Media.Containers.Riff.RiffReader.HasSubType(chunk)) Console.WriteLine("Type: " + Media.Containers.Riff.RiffReader.GetSubType(chunk));

                        Console.WriteLine("DataSize: " + chunk.DataSize);
                        Console.WriteLine("TotalSize: " + chunk.DataSize);
                    }

                    Console.WriteLine("File Level Information");

                    Console.WriteLine("Microseconds Per Frame:" + reader.MicrosecondsPerFrame);

                    Console.WriteLine("Max Bytes Per Seconds:" + reader.MaxBytesPerSecond);

                    Console.WriteLine("Flags:" + reader.Flags);
                    Console.WriteLine("HasIndex:" + reader.HasIndex);
                    Console.WriteLine("MustUseIndex:" + reader.MustUseIndex);
                    Console.WriteLine("IsInterleaved:" + reader.IsInterleaved);
                    Console.WriteLine("TrustChunkType:" + reader.TrustChunkType);
                    Console.WriteLine("WasCaptureFile:" + reader.WasCaptureFile);
                    Console.WriteLine("Copyrighted:" + reader.Copyrighted);

                    Console.WriteLine("Total Frames:" + reader.TotalFrames);

                    Console.WriteLine("Initial Frames:" + reader.InitialFrames);

                    Console.WriteLine("Streams:" + reader.Streams);

                    Console.WriteLine("Suggested Buffer Size:" + reader.SuggestedBufferSize);

                    Console.WriteLine("Width:" + reader.Width);

                    Console.WriteLine("Height:" + reader.Height);

                    Console.WriteLine("Reserved:" + reader.Reserved);

                    Console.WriteLine("Duration:" + reader.Duration);

                    Console.WriteLine("Created:" + reader.Created);

                    Console.WriteLine("Last Modified:" + reader.Modified);

                    Console.WriteLine("Track Information:");

                    foreach (var track in reader.GetTracks()) DumpTrack(track);
                }
            }

            if (System.IO.Directory.Exists(localPath + "/Media/Video/avi/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/avi/")) using (Media.Containers.Riff.RiffReader reader = new Media.Containers.Riff.RiffReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Chunk:" + Media.Containers.Riff.RiffReader.ToFourCharacterCode(reader.Root.Identifier));

                        Console.WriteLine("File Level Information");

                        Console.WriteLine("Microseconds Per Frame:" + reader.MicrosecondsPerFrame);

                        Console.WriteLine("Max Bytes Per Seconds:" + reader.MaxBytesPerSecond);

                        Console.WriteLine("Flags:" + reader.Flags);
                        Console.WriteLine("HasIndex:" + reader.HasIndex);
                        Console.WriteLine("MustUseIndex:" + reader.MustUseIndex);
                        Console.WriteLine("IsInterleaved:" + reader.IsInterleaved);
                        Console.WriteLine("TrustChunkType:" + reader.TrustChunkType);
                        Console.WriteLine("WasCaptureFile:" + reader.WasCaptureFile);
                        Console.WriteLine("Copyrighted:" + reader.Copyrighted);

                        Console.WriteLine("Total Frames:" + reader.TotalFrames);

                        Console.WriteLine("Initial Frames:" + reader.InitialFrames);

                        Console.WriteLine("Streams:" + reader.Streams);

                        Console.WriteLine("Suggested Buffer Size:" + reader.SuggestedBufferSize);

                        Console.WriteLine("Width:" + reader.Width);

                        Console.WriteLine("Height:" + reader.Height);

                        Console.WriteLine("Reserved:" + reader.Reserved);

                        Console.WriteLine("Duration:" + reader.Duration);

                        Console.WriteLine("Created:" + reader.Created);

                        Console.WriteLine("Last Modified:" + reader.Modified);

                        Console.WriteLine("Chunks:");

                        foreach (var chunk in reader)
                        {
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + chunk.Offset);
                            Console.WriteLine("DataOffset: " + chunk.DataOffset);
                            Console.WriteLine("Complete: " + chunk.IsComplete);

                            string name = Media.Containers.Riff.RiffReader.ToFourCharacterCode(chunk.Identifier);

                            Console.WriteLine("Name: " + name);

                            //Show how the common type can be read.
                            if (Media.Containers.Riff.RiffReader.HasSubType(chunk)) Console.WriteLine("Type: " + Media.Containers.Riff.RiffReader.GetSubType(chunk));

                            Console.WriteLine("DataSize: " + chunk.DataSize);
                            Console.WriteLine("TotalSize: " + chunk.DataSize);
                        }

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

            #endregion

            #region MatroskaReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/mkv/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/mkv/"))
                {
                    using (Media.Containers.Matroska.MatroskaReader reader = new Media.Containers.Matroska.MatroskaReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Element:" + reader.Root.ToString());

                        Console.WriteLine("File Level Information");

                        Console.WriteLine("EbmlVersion:" + reader.EbmlVersion);
                        Console.WriteLine("EbmlReadVersion:" + reader.EbmlReadVersion);
                        Console.WriteLine("DocType:" + reader.DocType);
                        Console.WriteLine("DocTypeVersion:" + reader.DocTypeVersion);
                        Console.WriteLine("DocTypeReadVersion:" + reader.DocTypeReadVersion);
                        Console.WriteLine("EbmlMaxIdLength:" + reader.EbmlMaxIdLength);
                        Console.WriteLine("EbmlMaxSizeLength:" + reader.EbmlMaxSizeLength);

                        Console.WriteLine("Elements:");

                        foreach (var element in reader)
                        {
                            Console.WriteLine("Name: " + element.ToString());
                            Console.WriteLine("Element Offset: " + element.Offset);
                            Console.WriteLine("Element Data Offset: " + element.DataOffset);
                            Console.WriteLine("Element DataSize: " + element.DataSize);
                            Console.WriteLine("Element TotalSize: " + element.TotalSize);
                            Console.WriteLine("Element.IsComplete: " + element.IsComplete);
                        }

                        Console.WriteLine("Movie Muxer Application:" + reader.MuxingApp);

                        Console.WriteLine("Movie Writing Applicatiopn:" + reader.WritingApp);

                        Console.WriteLine("Created:" + reader.Created);

                        Console.WriteLine("Modified:" + reader.Modified);

                        Console.WriteLine("Movie Duration:" + reader.Duration);

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);

                    }

                }

            #endregion

            #region AsfReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/asf/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/asf/"))
                {
                    using (Media.Containers.Asf.AsfReader reader = new Media.Containers.Asf.AsfReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Element:" + reader.Root.ToString());

                        Console.WriteLine("File Level Information");

                        Console.WriteLine("Created: " + reader.Created);
                        Console.WriteLine("Modified: " + reader.Modified);
                        Console.WriteLine("FileSize: " + reader.FileSize);
                        Console.WriteLine("NumberOfPackets: " + reader.NumberOfPackets);
                        Console.WriteLine("MinimumPacketSize: " + reader.MinimumPacketSize);
                        Console.WriteLine("MaximumPacketSize: " + reader.MaximumPacketSize);
                        Console.WriteLine("Duration: " + reader.Duration);
                        Console.WriteLine("PlayTime: " + reader.PlayTime);
                        Console.WriteLine("SendTime: " + reader.SendTime);
                        Console.WriteLine("PreRoll: " + reader.PreRoll);
                        Console.WriteLine("Flags: " + reader.Flags);
                        Console.WriteLine("IsBroadcast: " + reader.IsBroadcast);
                        Console.WriteLine("IsSeekable: " + reader.IsSeekable);

                        Console.WriteLine("Content Description");

                        Console.WriteLine("Title: " + reader.Title);
                        Console.WriteLine("Author: " + reader.Author);
                        Console.WriteLine("Copyright: " + reader.Copyright);
                        Console.WriteLine("Comment: " + reader.Comment);

                        Console.WriteLine("Objects:");

                        foreach (var asfObject in reader)
                        {
                            Console.WriteLine("Identifier:" + BitConverter.ToString(asfObject.Identifier));
                            Console.WriteLine("Name: " + asfObject.ToString());
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + asfObject.Offset);
                            Console.WriteLine("DataOffset: " + asfObject.DataOffset);
                            Console.WriteLine("Complete: " + asfObject.IsComplete);
                            Console.WriteLine("TotalSize: " + asfObject.TotalSize);
                            Console.WriteLine("DataSize: " + asfObject.DataSize);
                        }

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

                }

            #endregion

            #region MxfReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/mxf/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/mxf/"))
                {
                    using (Media.Containers.Mxf.MxfReader reader = new Media.Containers.Mxf.MxfReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Object:" + reader.Root.ToString());

                        Console.WriteLine("Objects:");

                        foreach (var mxfObject in reader)
                        {
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + mxfObject.Offset);
                            Console.WriteLine("DataOffset: " + mxfObject.DataOffset);
                            Console.WriteLine("Complete: " + mxfObject.IsComplete);

                            string name = Media.Containers.Mxf.MxfReader.ToTextualConvention(mxfObject.Identifier);

                            Console.WriteLine("Identifier: " + BitConverter.ToString(mxfObject.Identifier));

                            Console.WriteLine("Category: " + Media.Containers.Mxf.MxfReader.GetCategory(mxfObject));

                            Console.WriteLine("Name: " + name);

                            Console.WriteLine("TotalSize: " + mxfObject.TotalSize);
                            Console.WriteLine("DataSize: " + mxfObject.DataSize);

                            if (name == "PartitionPack")
                            {
                                Console.WriteLine("Partition Type: " + Media.Containers.Mxf.MxfReader.GetPartitionKind(mxfObject));
                                Console.WriteLine("Partition Status: " + Media.Containers.Mxf.MxfReader.GetPartitionStatus(mxfObject));
                            }
                        }

                        Console.WriteLine("File Level Properties");

                        Console.WriteLine("Created: " + reader.Created);

                        Console.WriteLine("Modified: " + reader.Modified);

                        Console.WriteLine("HasRunIn:" + reader.HasRunIn);

                        Console.WriteLine("RunInSize:" + reader.RunInSize);

                        Console.WriteLine("HeaderVersion:" + reader.HeaderVersion);

                        Console.WriteLine("AlignmentGrid:" + reader.AlignmentGridByteSize);

                        Console.WriteLine("IndexByteCount:" + reader.IndexByteCount);

                        Console.WriteLine("OperationalPattern:" + reader.OperationalPattern);

                        Console.WriteLine("ItemComplexity:" + reader.ItemComplexity);

                        Console.WriteLine("PrefaceLastModifiedDate:" + reader.PrefaceLastModifiedDate);

                        Console.WriteLine("PrefaceVersion:" + reader.PrefaceVersion);

                        Console.WriteLine("Platform:" + reader.Platform);

                        Console.WriteLine("CompanyName:" + reader.CompanyName);

                        Console.WriteLine("ProductName:" + reader.ProductName);

                        Console.WriteLine("ProductVersion:" + reader.ProductVersion);

                        Console.WriteLine("ProductUID:" + reader.ProductUID);

                        Console.WriteLine("IdentificationModificationDate:" + reader.IdentificationModificationDate);

                        Console.WriteLine("MaterialCreationDate:" + reader.Created);

                        Console.WriteLine("MaterialModifiedDate:" + reader.Modified);

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

                }

            #endregion

            #region OggReader

            Console.WriteLine("Url Test");

            //Really a mp4?
            location = new Uri("http://mirrorblender.top-ix.org/peach/bigbuckbunny_movies/big_buck_bunny_1080p_stereo.ogg");

            //Using a Download...
            using (var dl = Common.Extensions.Stream.StreamExtensions.HttpWebRequestDownload(location))
            {
                using (Media.Containers.Ogg.OggReader reader = new Media.Containers.Ogg.OggReader(location, dl, bufferSize))
                {
                    Console.WriteLine("Path:" + reader.Source);
                    Console.WriteLine("Total Size:" + reader.Length);

                    Console.WriteLine("Root Page:" + reader.Root.ToString());

                    Console.WriteLine("Pages:");

                    foreach (var page in reader)
                    {
                        //Buffer on demand...
                        while (reader.Buffering && reader.Remaining - page.TotalSize <= bufferSize)
                        {
                            Console.WriteLine("Buffering...");

                            Console.WriteLine("Length:" + reader.Length);

                            Console.WriteLine("Position:" + reader.Position);

                            //Expose wait handle..

                            System.Threading.Thread.Yield();
                        }

                        Console.WriteLine("Position:" + reader.Position);
                        Console.WriteLine("Offset: " + page.Offset);
                        Console.WriteLine("DataOffset: " + page.DataOffset);
                        Console.WriteLine("Complete: " + page.IsComplete);
                        Console.WriteLine("Name: " + page.ToString());
                        Console.WriteLine("HeaderFlags: " + Media.Containers.Ogg.OggReader.GetHeaderType(page));
                        Console.WriteLine("Size: " + page.TotalSize);
                    }


                    Console.WriteLine("File Level Properties");

                    Console.WriteLine("Created: " + reader.Created);

                    Console.WriteLine("Modified: " + reader.Modified);

                    Console.WriteLine("Track Information:");

                    foreach (var track in reader.GetTracks()) DumpTrack(track);
                }
            }

            if (System.IO.Directory.Exists(localPath + "/Media/Video/ogg/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/ogg/"))
                {
                    using (Media.Containers.Ogg.OggReader reader = new Media.Containers.Ogg.OggReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Page:" + reader.Root.ToString());

                        Console.WriteLine("Pages:");

                        foreach (var page in reader)
                        {
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + page.Offset);
                            Console.WriteLine("DataOffset: " + page.DataOffset);
                            Console.WriteLine("Complete: " + page.IsComplete);
                            Console.WriteLine("Name: " + page.ToString());
                            Console.WriteLine("HeaderFlags: " + Media.Containers.Ogg.OggReader.GetHeaderType(page));
                            Console.WriteLine("Size: " + page.TotalSize);
                        }


                        Console.WriteLine("File Level Properties");

                        Console.WriteLine("Created: " + reader.Created);

                        Console.WriteLine("Modified: " + reader.Modified);

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

                }

            #endregion

            #region NutReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/nut/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/nut/"))
                {
                    using (Media.Containers.Nut.NutReader reader = new Media.Containers.Nut.NutReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Tag:" + Media.Containers.Nut.NutReader.ToTextualConvention(reader.Root.Identifier));

                        Console.WriteLine("Tags:");

                        foreach (var tag in reader)
                        {
                            Console.WriteLine("Position:" + reader.Position);
                            Console.WriteLine("Offset: " + tag.Offset);
                            Console.WriteLine("DataOffset: " + tag.DataOffset);
                            Console.WriteLine("Complete: " + tag.IsComplete);

                            if (Media.Containers.Nut.NutReader.IsFrame(tag))
                            {
                                Console.WriteLine("Frame:");
                                Console.WriteLine("FrameFlags: " + Media.Containers.Nut.NutReader.GetFrameFlags(reader, tag));
                                int streamId = Media.Containers.Nut.NutReader.GetStreamId(tag);
                                Console.WriteLine("StreamId: " + streamId);
                                Console.WriteLine("HeaderOptions: " + reader.HeaderOptions[streamId]);
                                Console.WriteLine("FrameHeader: " + BitConverter.ToString(Media.Containers.Nut.NutReader.GetFrameHeader(reader, tag)));
                            }
                            else
                                Console.WriteLine("Name: " + Media.Containers.Nut.NutReader.ToTextualConvention(tag.Identifier));

                            Console.WriteLine("TotalSize: " + tag.TotalSize);
                            Console.WriteLine("DataSize: " + tag.DataSize);
                        }

                        Console.WriteLine("File Level Properties");

                        Console.WriteLine("File Id String:" + reader.FileIdString);

                        Console.WriteLine("Created: " + reader.Created);

                        Console.WriteLine("Modified: " + reader.Modified);

                        Console.WriteLine("Version:" + reader.Version);

                        Console.WriteLine("IsStableVersion:" + reader.IsStableVersion);

                        if (reader.HasMainHeaderFlags) Console.WriteLine("HeaderFlags:" + reader.MainHeaderFlags);

                        Console.WriteLine("Stream Count:" + reader.StreamCount);

                        Console.WriteLine("MaximumDistance:" + reader.MaximumDistance);

                        Console.WriteLine("TimeBases:" + reader.TimeBases.Count());

                        Console.WriteLine("EllisionHeaderCount:" + reader.EllisionHeaderCount);

                        Console.WriteLine("HeaderOptions:" + reader.HeaderOptions.Count());

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);
                    }

                }

            #endregion

            #region McfReader

            #endregion

            #region PacketizedElementaryStreamReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/pes/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/pes/"))
                {
                    using (Media.Containers.Mpeg.PacketizedElementaryStreamReader reader = new Media.Containers.Mpeg.PacketizedElementaryStreamReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Element:" + reader.Root.ToString());

                        Console.WriteLine("Packets:");

                        foreach (var pesPacket in reader)
                        {
                            Console.WriteLine(pesPacket.ToString());
                            Console.WriteLine("Packets Offset: " + pesPacket.Offset);
                            Console.WriteLine("Packets Data Offset: " + pesPacket.DataOffset);
                            Console.WriteLine("Packets DataSize: " + pesPacket.DataSize);
                            Console.WriteLine("Packets TotalSize: " + pesPacket.TotalSize);
                            Console.WriteLine("Packet.IsComplete: " + pesPacket.IsComplete);
                        }

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);

                    }

                }

            #endregion

            #region ProgramStreamReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/ps/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/ps/"))
                {
                    using (Media.Containers.Mpeg.ProgramStreamReader reader = new Media.Containers.Mpeg.ProgramStreamReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Element:" + reader.Root.ToString());

                        Console.WriteLine("System Clock Rate:" + reader.SystemClockRate);

                        Console.WriteLine("Packets:");

                        foreach (var packet in reader)
                        {
                            Console.WriteLine(packet.ToString());
                            Console.WriteLine("Element Offset: " + packet.Offset);
                            Console.WriteLine("Element Data Offset: " + packet.DataOffset);
                            Console.WriteLine("Element DataSize: " + packet.DataSize);
                            Console.WriteLine("Element TotalSize: " + packet.TotalSize);
                            Console.WriteLine("Element.IsComplete: " + packet.IsComplete);
                        }

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);

                    }

                }

            #endregion

            #region TransportStreamReader

            if (System.IO.Directory.Exists(localPath + "/Media/Video/ts/")) foreach (string fileName in System.IO.Directory.GetFiles(localPath + "/Media/Video/ts/"))
                {
                    using (Media.Containers.Mpeg.TransportStreamReader reader = new Media.Containers.Mpeg.TransportStreamReader(fileName))
                    {
                        Console.WriteLine("Path:" + reader.Source);
                        Console.WriteLine("Total Size:" + reader.Length);

                        Console.WriteLine("Root Element:" + reader.Root.ToString());

                        Console.WriteLine("Packets:");

                        foreach (var tsUnit in reader)
                        {
                            Console.WriteLine("Unit Type:" + tsUnit.ToString());
                            Console.WriteLine("Unit Offset: " + tsUnit.Offset);
                            Console.WriteLine("Unit Data Offset: " + tsUnit.DataOffset);
                            Console.WriteLine("Unit DataSize: " + tsUnit.DataSize);
                            Console.WriteLine("Unit TotalSize: " + tsUnit.TotalSize);
                            Console.WriteLine("Unit.IsComplete: " + tsUnit.IsComplete);
                            Console.WriteLine("PacketIdentifier: " + Media.Containers.Mpeg.TransportStreamReader.GetPacketIdentifier(reader, tsUnit.Identifier));
                            Console.WriteLine("Has Payload: " + Media.Containers.Mpeg.TransportStreamReader.HasPayload(reader, tsUnit));
                            Console.WriteLine("HasTransportPriority: " + Media.Containers.Mpeg.TransportStreamReader.HasTransportPriority(reader, tsUnit));
                            Console.WriteLine("HasTransportErrorIndicator: " + Media.Containers.Mpeg.TransportStreamReader.HasTransportErrorIndicator(reader, tsUnit));
                            Console.WriteLine("HasPayloadUnitStartIndicator: " + Media.Containers.Mpeg.TransportStreamReader.HasPayloadUnitStartIndicator(reader, tsUnit));
                            Console.WriteLine("ScramblingControl: " + Media.Containers.Mpeg.TransportStreamReader.GetScramblingControl(reader, tsUnit));
                            Console.WriteLine("ContinuityCounter: " + Media.Containers.Mpeg.TransportStreamReader.GetContinuityCounter(reader, tsUnit));
                            // See section 2.4.3.3 of 13818-1
                            Media.Containers.Mpeg.TransportStreamReader.AdaptationFieldControl adaptationFieldControl = Media.Containers.Mpeg.TransportStreamReader.GetAdaptationFieldControl(reader, tsUnit);
                            Console.WriteLine("AdaptationFieldControl: " + adaptationFieldControl);
                            if (adaptationFieldControl >= Media.Containers.Mpeg.TransportStreamReader.AdaptationFieldControl.AdaptationFieldOnly)
                            {
                                Console.WriteLine("AdaptationField Flags: " + Media.Containers.Mpeg.TransportStreamReader.GetAdaptationFieldFlags(reader, tsUnit));
                                Console.WriteLine("AdaptationField Data : " + BitConverter.ToString(Media.Containers.Mpeg.TransportStreamReader.GetAdaptationFieldData(reader, tsUnit)));
                            }

                        }

                        Console.WriteLine("Track Information:");

                        foreach (var track in reader.GetTracks()) DumpTrack(track);

                    }

                }

            #endregion
        }

        static void RtspInspector()
        {
            var f = new Tests.RtspInspector();

            Application.Run(f);
        }

        static void ContainerInspector()
        {
            var f = new Tests.ContainerInspector();

            Application.Run(f);
        }

        #endregion

        /// <summary>
        /// Performs the Unit Tests for the Media.Common.Extensions.Encoding.EncodingExtensions class
        /// </summary>
        static void TestEncodingExtensions()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.EncodingExtensionsTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestTimer()
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.TimerTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestClock()
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.ClockTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestBus()
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.BusTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestStopWatch()
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.StopWatchTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestRtpFrame()
        {

            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.RtpFrameUnitTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestRFC2435Frame()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.RFC2435UnitTest));
        }
        
        /// <summary>
        /// 
        /// </summary>
        static void TestRtpRtcp()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtpRtcpTests));

            //prerequesite of RtpPacket

            //RtpHeader

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtpHeaderUnitTests));

            //RtpExtension

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtpExtensionUnitTests));

            //RtpPacket

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtpPacketUnitTests));

            //RtcpHeader

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpHeaderUnitTests));

            //RtcpPacket

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpPacketUnitTests));

            //prerequesite of RtcpReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpReportBlockUnitTests));

            //SendersReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpSendersReportUnitTests));

            //ReceiversReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpReceiversReportUnitTests));

            //prerequesite of RtcpSourceDescriptionReportUnitTests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(SourceDescriptionItemUnitTests));

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(SourceDescriptionChunkUnitTests));

            //SourceDescriptionReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpSourceDescriptionReportUnitTests));

            //GoodbyeReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpGoodbyeReportUnitTests));

            //ApplicationSpecificReport

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(RtcpApplicationSpecificReportUnitTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestRtspMessage()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.RtspMessgeUnitTests));
        }

        /// <summary>
        /// 
        /// </summary>
        static void TestHttpMessage()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.HttpMessgeUnitTests));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void TestBinary()
        {

            Console.WriteLine("Detected a: " + Media.Common.Binary.SystemByteOrder.ToString() + ' ' + Media.Common.Binary.SystemByteOrder.GetType().Name + " System.");

            Console.WriteLine("Detected a: " + Media.Common.Binary.SystemBinaryRepresentation.ToString() + ' ' + Media.Common.Binary.SystemBinaryRepresentation.GetType().Name + " System.");

            Console.WriteLine("Detected a: " + Media.Common.Binary.SystemBitOrder.ToString() + ' ' + Media.Common.Binary.SystemBitOrder.GetType().Name + " System.");

            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(Media.UnitTests.BinaryUnitTests));
        }

        //static void TestSocketConfiguration()
        //{
        //    //Perform the tests
        //    CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(SocketConfigurationUnitTests));
        //}

        static void TestIPNetworkConnection()
        {
            using (Media.Sockets.IPNetworkConnection IPNetworkConnection = new Media.Sockets.IPNetworkConnection("aol.com"))
            {

                //System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                //System.Net.NetworkInformation.NetworkInterface networkInterface = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(nc.HostEntry.AddressList.First());

                System.Console.WriteLine("Established: " + IPNetworkConnection.IsEstablished);

                System.Console.WriteLine("HostEntry.HostName: " + IPNetworkConnection.RemoteIPHostEntry.HostName);

                //nc.Connect(0, networkInterface);

                IPNetworkConnection.Connect(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp, IPNetworkConnection.RemoteIPHostEntry.AddressList, 80);

                System.Console.WriteLine("Established: " + IPNetworkConnection.IsEstablished);

                System.Console.WriteLine("NetworkInterface.Name: " + IPNetworkConnection.NetworkInterface.Name);

                System.Console.WriteLine("LocalEndPoint: " + IPNetworkConnection.LocalEndPoint);

                System.Console.WriteLine("RemoteEndPoint: " + IPNetworkConnection.RemoteEndPoint);

                IPNetworkConnection.Refresh();

                IPNetworkConnection.Disconnect();

                System.Console.WriteLine("Established: " + IPNetworkConnection.IsEstablished);

            }
        }

        static void TestMachine()
        {
            //Perform the tests
            CreateInstanceAndInvokeAllMethodsWithReturnType(typeof(MachineUnitTests));
        }

        #endregion

        #region Methods (To Support Unit Tests)

        static Type typeOfVoid = typeof(void);

        static void CreateInstanceAndInvokeAllMethodsWithReturnType(Type instanceType)
        {
            CreateInstanceAndInvokeAllMethodsWithReturnType(instanceType, typeOfVoid, true);
        }

        static void CreateInstanceAndInvokeAllMethodsWithReturnType(Type instanceType, Type returnType, bool writeNames = true)
        {
            Object typedInstance = Activator.CreateInstance(instanceType);

            //Write the name if desired
            if (writeNames) writeInfo("Testing Type: " + instanceType.Name);

            //Get the methods of the class
            foreach (var method in instanceType.GetMethods())
            {
                //Ensure for the void type
                if (method.ReturnType != returnType) continue;

                //Write the name if desired
                if (writeNames) writeInfo("Testing Method: " + method.Name);

                //Invoke the void with (no parameters) on the created instance
                method.Invoke(typedInstance, null);
            }

            typedInstance = null;
        }

        static void writeError(Exception ex)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("Test Failed!");
            Console.WriteLine("Exception.Message: " + ex.Message);
            Console.WriteLine("Press (W) to try again or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void writeInfo(string message, ConsoleColor? backgroundColor = null, ConsoleColor? foregroundColor= null)
        {

            ConsoleColor? previousBackgroundColor = null, previousForegroundColor = null;

            if (backgroundColor.HasValue)
            {
                previousBackgroundColor = Console.BackgroundColor;
                Console.BackgroundColor = backgroundColor.Value;
            }

            if (foregroundColor.HasValue)
            {
                previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = foregroundColor.Value;
            }

            Console.WriteLine(message);

            if (previousBackgroundColor.HasValue)
            {
                Console.BackgroundColor = previousBackgroundColor.Value;
            }

            if (previousForegroundColor.HasValue)
            {
                Console.ForegroundColor = previousForegroundColor.Value;
            }
        }

        static void TraceMessage(string message, string memberName = "",
          [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
          [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine(string.Format(TestingFormat, message, memberName ?? "No MethodName Provided"));
        }

        static void RunTest(Action test, int count = 1, bool waitForGoAhead = true)
        {
            System.Console.Clear();
            ConsoleColor pForeGround = Console.ForegroundColor,
                        pBackGound = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine("About to run test: " + test.Method.Name);
            Console.WriteLine("Press Q to skip or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;

            //If the debugger is attached get a ConsoleKey, the key is Q return.
            if (waitForGoAhead && Console.ReadKey(true).Key == ConsoleKey.Q) return;
            else
            {
                Dictionary<int, Exception> log = null;

                int remaining = count, failures = 0, successes = 0; bool multipleTests = count > 1;

                if (multipleTests) log = new Dictionary<int, Exception>();

                foreach (var testIndex in Enumerable.Range(0, count).AsParallel())
                {
                    try
                    {

                        //Decrement remaining
                        --remaining;

                        TraceMessage("Beginning Test '" + testIndex + "'", test.Method.Name);

                        //Run the test
                        test();

                        TraceMessage("Completed Test'" + testIndex + "'", test.Method.Name);

                        //Increment the success counter
                        ++successes;
                    }
                    catch (Exception ex)
                    {
                        //Incrment the exception counter
                        ++failures;

                        //Only break if the debugger is attached
                        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();

                        //Write the exception to the console
                        writeError(ex);

                        //If there were multiple tests
                        if (multipleTests)
                        {
                            //Add the exception to the log
                            log.Add(remaining, ex);
                        }
                    }
                }

                //Write the amount of failures and successes unless all tests passed
                if (failures == 0) writeInfo("\tAll '" + count + "' Tests Passed!\r\n\tPress (W) To Run Again, (D) to Debug or any other key to continue.", null, ConsoleColor.Green);
                else writeInfo("\t" + failures + " Failures, " + successes + " Successes", null, failures > 0 ? ConsoleColor.Red : ConsoleColor.Green);

                //Oops core lib
                //var logFile in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(executingAssemblyLocation) + "*.log.txt")

                //Delete log.txt files.
                foreach (var logFile in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(executingAssemblyLocation), "*.log.txt"))
                {
                    try { System.IO.File.Delete(logFile); }
                    catch (Exception)
                    {
                        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                        continue;
                    }
                }

                ConsoleKey input = Console.ReadKey(true).Key;
                switch (input)
                {
                    case ConsoleKey.W:
                        {
                            RunTest(test, 1, false);
                            return;
                        }
                    case ConsoleKey.D:
                        {
                            //If the debugger is attached Read a ConsoleKey. (intercepting the key so it does not appear on the console)
                            if (System.Diagnostics.Debugger.IsAttached)
                            {
                                System.Diagnostics.Debugger.Break(); goto default;
                            }

                            break;
                        }
                    default: break;
                }

            }

            Console.BackgroundColor = pBackGound;

            Console.ForegroundColor = pForeGround;

        }

        internal static void TryPrintPacket(bool incomingFlag, Media.Common.IPacket packet, bool writePayload = false, Media.Rtp.RtpClient.TransportContext tc = null) { TryPrintClientPacket(null, incomingFlag, packet, writePayload, tc); }

        internal static void TryPrintClientPacket(object sender, bool incomingFlag, Media.Common.IPacket packet, bool writePayload = false, Media.Rtp.RtpClient.TransportContext tc = null)
        {
            if (sender is Media.Rtp.RtpClient && (sender as Media.Rtp.RtpClient).IsDisposed) return;

            ConsoleColor previousForegroundColor = Console.ForegroundColor,
                    previousBackgroundColor = Console.BackgroundColor;

            string format = "{0} a {1} {2}";

            Type packetType = packet.GetType();

            if (packet is Media.Rtp.RtpPacket)
            {
                Media.Rtp.RtpPacket rtpPacket = packet as Media.Rtp.RtpPacket;

                if (packet == null || packet.IsDisposed) return;

                if (packet.IsComplete) Console.ForegroundColor = ConsoleColor.Blue;
                else Console.ForegroundColor = ConsoleColor.Red;

                Media.Rtp.RtpClient client = ((Media.Rtp.RtpClient)sender);

                Media.Rtp.RtpClient.TransportContext matched = null;

                if (client != null) matched = tc ?? client.GetContextForPacket(rtpPacket);

                if (matched == null)
                {
                    Console.WriteLine("****Unknown RtpPacket context: " + Media.RtpTools.RtpSendExtensions.PayloadDescription(rtpPacket) + '-' + rtpPacket.PayloadType + " Length = " + rtpPacket.Length + (rtpPacket.Header.IsCompressed ? string.Empty : "Ssrc " + rtpPacket.SynchronizationSourceIdentifier.ToString()) + " \nAvailables Contexts:", "*******\n\t***********");
                    if (client != null) foreach (Media.Rtp.RtpClient.TransportContext tcc in client.GetTransportContexts())
                        {
                            Console.WriteLine(string.Format(TestingFormat, "\tDataChannel", tcc.DataChannel));
                            Console.WriteLine(string.Format(TestingFormat, "\tControlChannel", tcc.ControlChannel));
                            Console.WriteLine(string.Format(TestingFormat, "\tLocalSourceId", tcc.SynchronizationSourceIdentifier));
                            Console.WriteLine(string.Format(TestingFormat, "\tRemoteSourceId", tcc.RemoteSynchronizationSourceIdentifier));
                        }
                }
                else
                {
                    Console.WriteLine(string.Format(TestingFormat, "Matches Context (By PayloadType):", "*******\n\t***********Local Id: " + matched.SynchronizationSourceIdentifier + " Remote Id:" + matched.RemoteSynchronizationSourceIdentifier));
                    Console.WriteLine(string.Format(format, incomingFlag ? "\tReceieved" : "\tSent", (packet.IsComplete ? "Complete" : "Incomplete"), packetType.Name) + "\tSequenceNo = " + rtpPacket.SequenceNumber + " Timestamp=" + rtpPacket.Timestamp + " PayloadType = " + rtpPacket.PayloadType + " " + Media.RtpTools.RtpSendExtensions.PayloadDescription(rtpPacket) + " Length = " +
                        rtpPacket.Length + "\nContributingSourceCount = " + rtpPacket.ContributingSourceCount
                        + "\n Version = " + rtpPacket.Version + "\tSynchronizationSourceIdentifier = " + rtpPacket.SynchronizationSourceIdentifier);
                }
                if (rtpPacket.Payload.Count > 0 && writePayload) Console.WriteLine(string.Format(TestingFormat, "Payload", BitConverter.ToString(rtpPacket.Payload.Array, rtpPacket.Payload.Offset, rtpPacket.Payload.Count)));
            }
            else
            {
                if (packet == null || packet.IsDisposed) return;

                Media.Rtcp.RtcpPacket rtcpPacket = packet as Media.Rtcp.RtcpPacket;

                if (packet.IsComplete) if (packet.Transferred.HasValue) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.DarkGreen;
                else Console.ForegroundColor = ConsoleColor.Yellow;

                Media.Rtp.RtpClient client = ((Media.Rtp.RtpClient)sender);

                Media.Rtp.RtpClient.TransportContext matched = null;

                if (client != null) matched = tc ?? client.GetContextForPacket(rtcpPacket);

                Type implemented = Media.Rtcp.RtcpPacket.GetImplementationForPayloadType(rtcpPacket.PayloadType);

                if (implemented != null) packetType = implemented;

                Console.WriteLine(string.Format(format, incomingFlag ? "\tReceieved" : "\tSent", (packet.IsComplete ? "Complete" : "Incomplete"), packetType.Name) + "\tSynchronizationSourceIdentifier=" + rtcpPacket.SynchronizationSourceIdentifier + "\nType=" + rtcpPacket.PayloadType + " Length=" + rtcpPacket.Length + "\n Bytes = " + rtcpPacket.Payload.Count + " BlockCount = " + rtcpPacket.BlockCount + "\n Version = " + rtcpPacket.Version);

                if (rtcpPacket.Payload.Count > 0 && writePayload) Console.WriteLine(string.Format(TestingFormat, "Payload", BitConverter.ToString(rtcpPacket.Payload.Array, rtcpPacket.Payload.Offset, rtcpPacket.Payload.Count)));

                if (matched != null) Console.WriteLine(string.Format(TestingFormat, "Context:", "*******\n\t*********** Local Id: " + matched.SynchronizationSourceIdentifier + " Remote Id:" + matched.RemoteSynchronizationSourceIdentifier + " - Channel = " + matched.ControlChannel));
                else
                {
                    Console.WriteLine(string.Format(TestingFormat, "Unknown RTCP Packet context -> " + rtcpPacket.PayloadType + " \nAvailables Contexts:", "*******\n\t***********"));
                    if (client != null) foreach (Media.Rtp.RtpClient.TransportContext tcc in client.GetTransportContexts())
                        {
                            Console.WriteLine(string.Format(TestingFormat, "\tDataChannel", tcc.DataChannel));
                            Console.WriteLine(string.Format(TestingFormat, "\tControlChannel", tcc.ControlChannel));
                            Console.WriteLine(string.Format(TestingFormat, "\tLocalSourceId", tcc.SynchronizationSourceIdentifier));
                            Console.WriteLine(string.Format(TestingFormat, "\tRemoteSourceId", tcc.RemoteSynchronizationSourceIdentifier));
                        }
                }

                if (implemented != null)
                {
                    //Could dump the packet contents here.
                    Console.WriteLine(Media.RtpTools.RtpSend.ToTextualConvention(Media.RtpTools.FileFormat.Ascii, rtcpPacket));
                }


            }

            Console.ForegroundColor = previousForegroundColor;
            Console.BackgroundColor = previousBackgroundColor;

        }

        static void PrintRtcpInformation(Media.Rtcp.RtcpPacket p)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            TryPrintPacket(true, p);
            //Console.WriteLine("RTCP Packet Version:" + p.Version + "Length =" + p.Length + " Bytes: " + BitConverter.ToString(p.Prepare().ToArray(), 0, Math.Min(Console.BufferWidth, p.Length)));
            Console.BackgroundColor = ConsoleColor.Black;

            //Dissect the packet
            switch (p.PayloadType)
            {
                case Media.Rtcp.SendersReport.PayloadType:
                    {
                        Console.WriteLine(string.Format(TestingFormat, "SendersReport From", p.SynchronizationSourceIdentifier));
                        Console.WriteLine(string.Format(TestingFormat, "Length", p.Length));

                        using (Media.Rtcp.SendersReport sr = new Media.Rtcp.SendersReport(p, false))
                        {
                            Console.WriteLine(string.Format(TestingFormat, "NtpTime", sr.NtpTime));

                            Console.WriteLine(string.Format(TestingFormat, "RtpTimestamp", sr.RtpTimestamp));

                            Console.WriteLine(string.Format(TestingFormat, "SendersOctetCount", sr.SendersOctetCount));

                            Console.WriteLine(string.Format(TestingFormat, "SendersPacketCount", sr.SendersPacketCount));

                            //Enumerate any blocks in the senders report
                            using (var enumerator = sr.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    Media.Rtcp.ReportBlock asReportBlock = enumerator.Current as Media.Rtcp.ReportBlock;

                                    Console.WriteLine("Found a ReportBlock");

                                    Console.WriteLine("FractionsLost: " + asReportBlock.FractionsLost);
                                    Console.WriteLine("CumulativePacketsLost: " + asReportBlock.CumulativePacketsLost);
                                    Console.WriteLine("ExtendedHighestSequenceNumberReceived: " + asReportBlock.ExtendedHighestSequenceNumberReceived);
                                    Console.WriteLine("InterarrivalJitterEstimate: " + asReportBlock.InterarrivalJitterEstimate);
                                    Console.WriteLine("LastSendersReportTimestamp: " + asReportBlock.LastSendersReportTimestamp);
                                }
                            }
                        }

                        break;
                    }
                case Media.Rtcp.SourceDescriptionReport.PayloadType:
                    {
                        //Create a SourceDescriptionReport from the packet instance to access the SourceDescriptionChunks
                        using (Media.Rtcp.SourceDescriptionReport sourceDescription = new Media.Rtcp.SourceDescriptionReport(p, false))
                        {

                            Console.WriteLine(string.Format(TestingFormat, "SourceDescription From", sourceDescription.SynchronizationSourceIdentifier));
                            Console.WriteLine(string.Format(TestingFormat, "Length", p.Length));

                            foreach (var chunk in sourceDescription.GetChunkIterator())
                            {
                                Console.WriteLine(string.Format(TestingFormat, "Chunk Identifier", chunk.ChunkIdentifer));
                                //Use a SourceDescriptionItemList to access the items within the Chunk
                                //This is performed auto magically when using the foreach pattern
                                foreach (Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem item in chunk /*.AsEnumerable<Rtcp.SourceDescriptionItem>()*/)
                                {
                                    Console.WriteLine(string.Format(TestingFormat, "Item Type", item.ItemType));
                                    Console.WriteLine(string.Format(TestingFormat, "Item Length", item.ItemLength));
                                    Console.WriteLine(string.Format(TestingFormat, "Item Data", BitConverter.ToString(item.ItemData.ToArray())));
                                }
                            }
                        }
                        break;
                    }
                default: Console.WriteLine(Media.RtpTools.RtpSend.ToTextualConvention(Media.RtpTools.FileFormat.Ascii, p)); break;

            }
        }

        internal static void SendKeepAlive(Media.Rtsp.RtspClient client, object state = null)
        {
            if (client == null || client.IsDisposed) return;

            Console.WriteLine(client.InternalId + " - Sending KeepAlive");

            client.SendKeepAliveRequest(state);
        }

        internal static void SendRandomPartial(Media.Rtsp.RtspClient client, Media.Rtsp.RtspMethod method = Media.Rtsp.RtspMethod.GET_PARAMETER, Uri location = null, string contentType = null, byte[] data = null)
        {
            if (client == null || client.IsDisposed) return;

            if (false == client.IsConnected)
            {
                Console.WriteLine(client.InternalId + " - Client Not Connected, Connect First!");

                return;
            }

            Console.WriteLine(client.InternalId + " - Sending Partial " + method);

            using (Media.Rtsp.RtspMessage message = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request)
            {
                RtspMethod = method,
                Location = location ?? Media.Rtsp.RtspMessage.Wildcard
            })
            {

                message.SetHeader(Media.Rtsp.RtspHeaders.Session, client.SessionId);

                message.SetHeader(Media.Rtsp.RtspHeaders.CSeq, client.NextClientSequenceNumber().ToString());

                byte[] buffer;

                if (data == null)
                {
                    buffer = new byte[Utility.Random.Next(0, Media.Rtsp.RtspMessage.MaximumLength)];

                    Utility.Random.NextBytes(buffer);

                    message.Body = message.ContentEncoding.GetString(buffer);

                    message.SetHeader(Media.Rtsp.RtspHeaders.ContentEncoding, "application/octet-string");
                }
                else
                {
                    buffer = data;

                    message.ContentEncoding.GetString(data);

                    message.SetHeader(Media.Rtsp.RtspHeaders.ContentEncoding, contentType ?? "application/octet-string");
                }


                Media.Rtsp.RtspMessage parsed = Media.Rtsp.RtspMessage.FromString(message.ToString());

                int max = message.Length, toSend = Utility.Random.Next(client.Buffer.Count);

                if (toSend == max) using (client.SendRtspMessage(message)) ;
                else
                {
                    int sent = 0;
                    //Send only some of the data
                    do sent = client.RtspSocket.Send(buffer);
                    while (sent == 0);

                    string output = message.ContentEncoding.GetString(message.ToBytes(), 0, sent);

                    Console.WriteLine(client.InternalId + " - Sent Partial(" + sent + "/" + message.Length + "): " + output);

                    parsed.Dispose();

                    parsed = Media.Rtsp.RtspMessage.FromString(output);

                    Console.WriteLine(client.InternalId + " - Parsed: " + parsed);

                    //Send the real request with the same data
                    using (client.SendRtspMessage(message)) ;

                    parsed.Dispose();

                }
            }
        }

        internal static void DumpTrack(Media.Container.Track track)
        {
            Console.WriteLine("Id: " + track.Id);

            Console.WriteLine("Name: " + track.Name);
            Console.WriteLine("Duration: " + track.Duration);

            Console.WriteLine("Type: " + track.MediaType);
            Console.WriteLine("Samples: " + track.SampleCount);

            if (track.MediaType == Media.Sdp.MediaType.audio)
            {
                Console.WriteLine("Codec: " + (track.CodecIndication.Length > 2 ? Encoding.UTF8.GetString(track.CodecIndication) : ((Media.Codecs.Audio.WaveFormatId)Media.Common.Binary.ReadU16(track.CodecIndication, 0, false)).ToString()));
                Console.WriteLine("Channels: " + track.Channels);
                Console.WriteLine("Sampling Rate: " + track.Rate);
                Console.WriteLine("Bits Per Sample: " + track.BitDepth);
            }
            else
            {
                Console.WriteLine("Codec: " + Encoding.UTF8.GetString(track.CodecIndication));
                Console.WriteLine("Frame Rate: " + track.Rate);
                Console.WriteLine("Width: " + track.Width);
                Console.WriteLine("Height: " + track.Height);
                Console.WriteLine("BitsPerPixel: " + track.BitDepth);
            }
        }

        #endregion

        static string[] PublicHttpHosts = new string[]
        {
            "http://httpbin.org/",
            "http://httpbin.org/ip",
            "http://httpbin.org/headers",
            "http://httpbin.org/get",
            //Post

            "http://www.google.com",
            "http://www.microsoft.com",
            "http://www.apple.com",
        };

        static void TestHttpClient(string location, System.Net.NetworkCredential cred = null)
        {
            //Check for a location
            if (string.IsNullOrWhiteSpace(location))
            {
                Console.WriteLine("Enter a RTSP URL and press enter (Or enter to quit):");
                location = Console.ReadLine();
            }

            //Write information about the test to the console
            Console.WriteLine("Location = \"" + location + "\" " + "\n Press a key to continue. Press Q to Skip, Press B to set the buffer size.");

            //Define a HttpClient
            Media.Http.HttpClient client = null;

            ConsoleKey inKey;

            if ((inKey = Console.ReadKey().Key) == ConsoleKey.Q) return;

            int bufferSize = Media.Rtsp.RtspClient.DefaultBufferSize;

            if (inKey == ConsoleKey.B)
            {
                Console.WriteLine("Type the buffer size and press return:");
                try
                {
                    bufferSize = int.Parse(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    writeError(ex);
                }
            }

            //Using a new Media.HttpClient optionally with a specified buffer size (0 indicates use the MTU if possible)
            using (client = new Media.Http.HttpClient(location, bufferSize))
            {
                using (var request = new Media.Http.HttpMessage(Http.HttpMessageType.Request)
                {
                    HttpMethod = Http.HttpMethod.GET,
                    Location = client.InitialLocation
                })
                {

                    Console.WriteLine("Sending Http Request:");
                    Console.WriteLine(request);

                    using (var response = client.SendHttpMessage(request))
                    {
                        if (response != null)
                        {
                            Console.WriteLine("Received Http Response:");
                            Console.WriteLine(response);
                        }
                        else
                        {
                            Console.WriteLine("No Response.");
                        }
                    }
                }



                //Add events for Connect and Disconnect if desired

                //Add events for Message Sent and Received if desired

                //Send a Message and wait for a response

                //Send a message and do not wait for a response
            }

            //Using a new Media.HttpClient optionally with a specified buffer size (0 indicates use the MTU if possible)
            using (client = new Media.Http.HttpClient(location, bufferSize))
            {
                using (var request = new Media.Http.HttpMessage(Http.HttpMessageType.Request)
                {
                    HttpMethod = Http.HttpMethod.GET,
                    Location = new Uri(client.InitialLocation.PathAndQuery, UriKind.RelativeOrAbsolute),
                    Version = client.ProtocolVersion = 1.1
                })
                {

                    request.SetHeader(Media.Http.HttpHeaders.Host, client.InitialLocation.Host);

                    Console.WriteLine("Sending Http Request:");
                    Console.WriteLine(request);

                    using (var response = client.SendHttpMessage(request))
                    {
                        if (response != null)
                        {
                            Console.WriteLine("Received Http Response:");
                            Console.WriteLine(response);
                        }
                        else
                        {
                            Console.WriteLine("No Response.");
                        }
                    }
                }

                //Send in chunks
                using (client = new Media.Http.HttpClient(location, bufferSize))
                {
                    client.SendChunked = true;

                    //messages must have a Body to be sent in chunks...

                    using (var request = new Media.Http.HttpMessage(Http.HttpMessageType.Request)
                    {
                        HttpMethod = Http.HttpMethod.OPTIONS,
                        Location = new Uri(client.InitialLocation.PathAndQuery, UriKind.RelativeOrAbsolute),
                        Version = client.ProtocolVersion = 1.1
                    })
                    {

                        request.SetHeader(Media.Http.HttpHeaders.Host, client.InitialLocation.Host);

                        Console.WriteLine("Sending Http Request:");
                        Console.WriteLine(request);

                        using (var response = client.SendHttpMessage(request))
                        {
                            if (response != null)
                            {
                                Console.WriteLine("Received Http Response:");
                                Console.WriteLine(response);
                            }
                            else
                            {
                                Console.WriteLine("No Response.");
                            }
                        }
                    }

                    using (var request = new Media.Http.HttpMessage(Http.HttpMessageType.Request)
                    {
                        HttpMethod = Http.HttpMethod.HEAD,
                        Location = new Uri(client.InitialLocation.PathAndQuery, UriKind.RelativeOrAbsolute),
                        Version = client.ProtocolVersion = 1.1
                    })
                    {

                        request.SetHeader(Media.Http.HttpHeaders.Host, client.InitialLocation.Host);

                        Console.WriteLine("Sending Http Request:");
                        Console.WriteLine(request);

                        using (var response = client.SendHttpMessage(request))
                        {
                            if (response != null)
                            {
                                Console.WriteLine("Received Http Response:");
                                Console.WriteLine(response);
                            }
                            else
                            {
                                Console.WriteLine("No Response.");
                            }
                        }
                    }
                }



                //Add events for Connect and Disconnect if desired

                //Add events for Message Sent and Received if desired

                //Send a Message and wait for a response

                //Send a message and do not wait for a response
            }
        }

        public static void HttpClientTests()
        {
            foreach (string host in PublicHttpHosts)
            {

            TestStart:

                try
                {
                    TestHttpClient(host, default(System.Net.NetworkCredential));
                }
                catch (Exception ex)
                {
                    writeError(ex);
                }

                Console.WriteLine("Done. Press (W) to run the same test again, Press (Q) or anything else to progress to the next test.");

                ConsoleKey next = Console.ReadKey(true).Key;

                switch (next)
                {
                    default:
                    case ConsoleKey.Q: continue;
                    case ConsoleKey.W: goto TestStart;
                }
            }
        }
    }
}
