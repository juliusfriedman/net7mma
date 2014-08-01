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

namespace Tests
{
    public class Program
    {

        internal static string TestingFormat = "{0}:=>{1}";

        static Action[] Tests = new Action[] { TestUtility, TestBinary, TestRtpPacket, TestRtpExtension, TestRtpFrame, TestJpegFrame, TestRtcpPacket, TestRtcpPacketExamples, TestRtpTools, TestSdp, TestRtspMessage };

        [MTAThread]
        public static void Main(string[] args)
        {

            //Enable Shift / Control + Shift moving through tests, e.g. some type menu 
            foreach (Action test in Tests) RunTest(test);            

            RunTest(TestRtpClient, 777);

            RunTest(RtspClientTests);

            RunTest(WinRtspInspector);

            RunTest(TestServer);
        }

        public static void TestUtility() 
        {

            //Each octet reflects it's offset in hexidecimal
                                        // 0 - 9 in hex is the same as decimal
            byte[] haystack = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15 },
                //Something to look for in the above
                   needle = new byte[] { 0x14, 0x15 };

            //For all 20 bytes look for them in the ensure haystack starting at the beginning
            for (int offset = 0, test = 0, haystackLength = haystack.Length, count = haystackLength , needleBegin = 0, needleLength = needle.Length; 
                //Perform tests up to the highest value in haystack by design
                test < 20; ++test, offset = 0, count = haystackLength, needle[1] = (byte)test, needle[0] = (byte)(test - 1)) //increment the test and reset the offset each and count each time
            {
                //Look for the whole needle in the haystack
                int offsetAfterParsing = Utility.ContainsBytes(haystack, ref offset, ref count, needle, needleBegin, needleLength);

                //Get the pointer to the bytes which correspond to the match in total
                var match = haystack.Skip(offset > 0 ? offset : 0).Take(needleLength);

                //Check for an invalid result in the test
                if (!match.SequenceEqual(needle)
                            ||//Double check the result is valid by examining the first byte to be equal to the offset (which is by design)                        
                                match.Take(1).First() != offset) throw new Exception("Invalid result found!");                    
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

        public static void TestBinary()
        {

            //Test bit 0
            byte one = 1, testBits = Media.Common.Binary.ReverseU8(one);

            if (testBits != 128) throw new Exception("Bit 0 Not Correct");

            if (Media.Common.Binary.GetBit(ref testBits, 0) != true) throw new Exception("GetBit Does not Work");

            if (Media.Common.Binary.SetBit(ref testBits, 0, true) != true) throw new Exception("SetBit Does not Work");

            //Test Bit Methods from 1 - 8
            for (int i = 1, e = 8; i <= e; ++i)
            {
                //Only 1 bit should be set from 1 - 8
                byte bits = (byte)i;

                //Test readomg the bit
                if (Media.Common.Binary.GetBit(ref bits, i) != true) throw new Exception("GetBit Does not Work");

                //Set the same bit
                if (Media.Common.Binary.SetBit(ref bits, i, true) != true) throw new Exception("SetBit Does not Work");

                //If the value is not exactly the same then throw an exception
                if (bits != i || Media.Common.Binary.GetBit(ref bits, i) != true) throw new Exception("GetBit Does not Work");
            }

            //Use 8 octets, each write over-writes the previous written value
            byte[] Octets = new byte[8];

            //Test is binary, so test both ways, 0 and 1
            for(int i = 0; i < 2; ++i)
            {
                //First test uses writing Network Endian and reading System Endian, next test does the opposite
                bool reverse = i > 0;

                //65535 iterations uses 16 bits of a 32 bit integer
                for (ushort v = ushort.MinValue; v < ushort.MaxValue; ++v)
                {
                    Media.Common.Binary.WriteNetwork16(Octets, 0, reverse, v);

                    byte[] SystemBits = BitConverter.GetBytes(reverse ? (ushort)System.Net.IPAddress.HostToNetworkOrder((short)v) : v);

                    if (!SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("Incorrect bits when compared to SystemBits");
                    else if (Media.Common.Binary.ReadInteger(Octets, 0, 2, reverse) != v) throw new Exception("Can't read back what was written");

                    Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));

                }

                //Repeat the test using each permutation of 16 bits not yet tested within the 4 octets which provide an integer of 32 bits
                for (uint s = uint.MinValue; s <= uint.MaxValue / ushort.MaxValue; ++s)
                {
                    uint v = uint.MaxValue * s;
                    Media.Common.Binary.WriteNetwork32(Octets, 0, reverse, v);

                    byte[] SystemBits = BitConverter.GetBytes(reverse ? (uint)System.Net.IPAddress.HostToNetworkOrder((int)v) : v);

                    if (!SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("Incorrect bits when compared to SystemBits");
                    else if (Media.Common.Binary.ReadInteger(Octets, 0, 4, reverse) != v) throw new Exception("Can't read back what was written");

                    Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));
                }

                //Repeat the test using each permuation of 16 bits within the 8 octets which provide an integer of 64 bits.
                for (uint s = uint.MinValue; s <= uint.MaxValue / ushort.MaxValue; ++s)
                {
                    //The low 32 bits. (Already tested in the previous test)
                    ulong v = s * uint.MaxValue;

                    //Test the high 32 bits and the low 32 bits at once
                    v = v << 32 | v;
                    Media.Common.Binary.WriteNetwork64(Octets, 0, reverse, v);

                    byte[] SystemBits = BitConverter.GetBytes(reverse ? (ulong)System.Net.IPAddress.HostToNetworkOrder((long)v) : v);

                    if (!SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("Incorrect bits when compared to SystemBits");
                    else if ((ulong)Media.Common.Binary.ReadInteger(Octets, 0, 8, reverse) != v) throw new Exception("Can't read back what was written");

                    Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));
                }

            }
        }

        public static void RtspClientTests()
        {
            foreach (var TestObject in new[] 
            {
                new
                {
                    Uri = "rtsp://quicktime.uvm.edu:1554/waw/wdi05hs2b.mov", //Single media item
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,
                },
                new
                {
                    Uri = "rtsp://46.249.213.93/broadcast/gamerushtv-tablet.3gp", //Continous Stream
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,
                },
                new
                {
                    Uri = "rtsp://184.72.239.149/vod/mp4:BigBuckBunny_115k.mov", //Single media item
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,

                },
                new
                {
                    Uri = "rtsp://v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/video.3gp", //Single media item
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,
                },
                new
                {
                    Uri = "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp", //Single media item
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,
                },
                new
                {
                    Uri = string.Empty,
                    Creds = default(System.Net.NetworkCredential),
                    Proto = (Media.Rtsp.RtspClient.ClientProtocolType?)null,
                },

            })
            {

                Media.Rtsp.RtspClient.ClientProtocolType? proto = TestObject.Proto;

            TestStart:
                try
                {
                    ///Allow for disable of GetParameter (set m_RtspTimeout = 0)
                    TestRtspClient(TestObject.Uri, TestObject.Creds, proto);
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
            TestRtpClient(DateTime.UtcNow.Second % 2 == 0);
        }

        /// <summary>
        /// Tests the RtpClient.
        /// </summary>
        static void TestRtpClient(bool tcp = true)
        {

            //Start a test to send a single frame as quickly as possible to a single party.
            //Disconnect after sending said frame test the disposable implementation, packets not yet received will be lost at that time.
            using (System.IO.TextWriter consoleWriter = new System.IO.StreamWriter(Console.OpenStandardOutput()))
            {

                //Get the local interface address
                System.Net.IPAddress localIp = Utility.GetFirstV4IPAddress();

                //Using a sender
                using (var sender = Media.Rtp.RtpClient.Sender(localIp))
                {
                    //Create a Session Description
                    Media.Sdp.SessionDescription SessionDescription = new Media.Sdp.SessionDescription(1);

                    //Add a MediaDescription to our Sdp on any port 17777 for RTP/AVP Transport using the RtpJpegPayloadType
                    SessionDescription.Add(new Media.Sdp.MediaDescription(Media.Sdp.MediaType.video, 17777, (tcp ? "TCP/" : string.Empty) + Media.Rtsp.Server.Streams.RtpSource.RtpMediaProtocol, Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame.RtpJpegPayloadType));

                    sender.m_TransportProtocol = System.Net.Sockets.ProtocolType.Tcp;

                    sender.RtcpPacketSent += (s, p) => TryPrintClientPacket(s, false, p);
                    sender.RtcpPacketReceieved += (s, p) => TryPrintClientPacket(s, true, p);
                    sender.RtpPacketSent += (s, p) => TryPrintClientPacket(s, false, p);

                    //Using a receiver
                    using (var receiver = Media.Rtp.RtpClient.Participant(Utility.GetFirstV4IPAddress()))
                    {

                        //Set tcp 
                        if (tcp)
                        {
                            sender.m_TransportProtocol = receiver.m_TransportProtocol = System.Net.Sockets.ProtocolType.Tcp;
                        }

                        //Determine when the sender and receive should time out
                        //sender.InactivityTimeout = receiver.InactivityTimeout = TimeSpan.FromSeconds(7);

                        receiver.RtcpPacketSent += (s, p) => TryPrintClientPacket(s, false, p);
                        receiver.RtcpPacketReceieved += (s, p) => TryPrintClientPacket(s, true, p);
                        receiver.RtpPacketReceieved += (s, p) => TryPrintClientPacket(s, true, p);

                        //Create and Add the required TransportContext's

                        int sendersId = RFC3550.Random32(Media.Rtcp.SendersReport.PayloadType), receiversId = sendersId + 1;

                        //Create two transport contexts, one for the sender and one for the receiver.
                        //The Id of the parties must be known in advance in this stand alone example. (A conference would support more then 1 participant)
                        Media.Rtp.RtpClient.TransportContext sendersContext = new Media.Rtp.RtpClient.TransportContext(0, 1, sendersId, SessionDescription.MediaDescriptions[0], true, receiversId),
                            receiversContext = new Media.Rtp.RtpClient.TransportContext(0, 1, receiversId, SessionDescription.MediaDescriptions[0], true, sendersId);

                        if (tcp) consoleWriter.WriteLine("TCP TEST");
                        else consoleWriter.WriteLine("UDP TEST");

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - " + sender.m_Id + " - Senders SSRC = " + sendersContext.SynchronizationSourceIdentifier);

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - " + receiver.m_Id + " - Recievers SSRC = " + receiversContext.SynchronizationSourceIdentifier);

                        //Find open ports, 1 for Rtp, 1 for Media.Rtcp
                        int incomingRtpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17777, false), rtcpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17778),
                        ougoingRtpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10777, false), xrtcpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10778);

                        //Initialzie the sockets required and add the context so the RtpClient can maintin it's state, once for the receiver and once for the sender in this example.
                        //Most application would only have one or the other.

                        receiversContext.InitializeSockets(localIp, localIp, incomingRtpPort, rtcpPort, ougoingRtpPort, xrtcpPort);

                        receiver.Add(receiversContext);

                        sendersContext.InitializeSockets(localIp, localIp, ougoingRtpPort, xrtcpPort, incomingRtpPort, rtcpPort);

                        sender.Add(sendersContext);

                        //Connect the sender
                        sender.Connect();

                        //Connect the reciver (On the `otherside`)
                        receiver.Connect();

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + " - Connection Established,  Encoding Frame");

                        //Make a frame
                        Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame testFrame = new Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame(new System.IO.FileStream("video.jpg", System.IO.FileMode.Open), 25, (int)sendersContext.SynchronizationSourceIdentifier, 0, (long)Utility.DateTimeToNptTimestamp(DateTime.UtcNow));

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + "Sending Encoded Frame");

                        int ts = 3600;

                        foreach (Media.Rtp.RtpPacket r in testFrame.Skip(1))
                        {
                            r.Timestamp += ts;
                            ts += 3600;
                        }

                        //Send it
                        sender.SendRtpFrame(testFrame);

                        //Wait for the frame to be sent only once
                        while (testFrame.Transferred == false || sendersContext.SendersReport == null || sendersContext.SendersReport.Transferred == null) System.Threading.Thread.Yield();

                        consoleWriter.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId + "\t *** Sent RtpFrame, Sending Reports and Goodbye ***");

                        //Wait for packets to be received
                        while (receiver.Connected && (receiversContext.ReceiversReport == null || receiversContext.ReceiversReport.Transferred == null)) System.Threading.Thread.Yield();

                        //Measure QoE / QoS based on sent / received ratio.
                        consoleWriter.WriteLine("\t Since : " + sendersContext.SendersReport.Transferred);
                        consoleWriter.WriteLine("\t -----------------------");
                        consoleWriter.WriteLine("\t Sender Sent : " + sendersContext.SendersReport.SendersPacketCount + " Packets");

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


        static void writeError(Exception ex)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("Test Failed!");
            Console.WriteLine("Exception.Message: " + ex.Message);
            Console.WriteLine("Press (A) to try again or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void writeInfo(string message, ConsoleColor? backgroundColor, ConsoleColor? foregroundColor)
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
            if (waitForGoAhead && System.Diagnostics.Debugger.IsAttached && Console.ReadKey(true).Key == ConsoleKey.Q) return;
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
                        System.Threading.Interlocked.Decrement(ref remaining);

                        TraceMessage("Beginning Test '" + testIndex + "'", test.Method.Name);

                        //Run the test
                        test();

                        TraceMessage("Completed Test'" + testIndex + "'", test.Method.Name);

                        //Increment the success counter
                        System.Threading.Interlocked.Increment(ref successes);
                    }
                    catch (Exception ex)
                    {
                        //Incrment the exception counter
                        System.Threading.Interlocked.Increment(ref failures);

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
                if (failures == 0) writeInfo("\tAll '"+ count + "' Tests Passed!\r\n\tPress (W) To Run Again, (D) to Debug or any other key to continue.", null, ConsoleColor.Green);
                else writeInfo("\t" + failures + " Failures, " + successes + " Successes", null, failures > 0 ? ConsoleColor.Red : ConsoleColor.Green);

                //If the debugger is attached Read a ConsoleKey. (intercepting the key so it does not appear on the console)
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    ConsoleKey input = Console.ReadKey(true).Key;
                    switch (input)
                    {
                        case ConsoleKey.W:
                            {
                                RunTest(test, 1, false);
                                return;
                            }
                        case ConsoleKey.D: System.Diagnostics.Debugger.Break(); goto default;
                        default: break;
                    }                    
                }
            }


            Console.BackgroundColor = pBackGound;

            Console.ForegroundColor = pForeGround;

        }

        static void TestRtcpPacketExamples()
        {

            byte[] output;

            //Keep a copy of these exceptions to throw in case some error occurs.
            Exception invalidLength = new Exception("Invalid Length"), invalidData = new Exception("Invalid Data in packet"), invalidPadding = new Exception("Invalid Padding"), incompleteFalse = new Exception("Packet IsComplete is false");

            //Create a Media.RtcpPacket with only a header (results in 8 octets of 0x00 which make up the header)
            Media.Rtcp.RtcpPacket rtcpPacket = new Media.Rtcp.RtcpPacket(0, 0, 0, 0, 0, 0);

            //Prepare a sequence which contains the data in the packet including the header
            IEnumerable<byte> preparedPacket = rtcpPacket.Prepare();

            //Check for an invlaid length
            if (rtcpPacket.Payload.Count > 0 || rtcpPacket.Header.LengthInWordsMinusOne != 0 && rtcpPacket.Length != 8 || preparedPacket.Count() != 8) throw invalidLength;

            //Check for any data in the packet binary
            if (preparedPacket.Any(o => o != default(byte))) throw invalidData;

            //Set padding in the header
            rtcpPacket.Padding = true;

            //Check for some invalid valid
            if (rtcpPacket.PaddingOctets > 0) throw invalidPadding;

            //Ensure the packet is complete
            if (rtcpPacket.IsComplete == false) throw incompleteFalse;

            //Add nothing to the payload
            rtcpPacket.AddBytesToPayload(RFC3550.CreatePadding(0), 0, 0);

            //Ensure the packet is complete
            if (rtcpPacket.IsComplete == false) throw incompleteFalse;

            //Check for some invalid value
            if (rtcpPacket.PaddingOctets > 0) throw invalidPadding;

            //Make a bunch of packets with padding
            for (int paddingAmount = 1, e = byte.MaxValue; paddingAmount <= e; ++paddingAmount)
            {

                //Write information for the test to the console
                Console.WriteLine(string.Format(TestingFormat, "Making Media.RtcpPacket with Padding", paddingAmount));

                //Try to make a padded packet with the given amount
                rtcpPacket = new Media.Rtcp.RtcpPacket(0, 0, paddingAmount, 0, 0, 0);

                //A a 4 bytes which are not padding related
                rtcpPacket.AddBytesToPayload(Enumerable.Repeat(default(byte), 4), 0, 1);

                //Check ReadPadding works after adding bytes to the payload
                if (rtcpPacket.PaddingOctets != paddingAmount) throw invalidPadding;

                //Ensure the packet is complete
                if (rtcpPacket.IsComplete == false) throw incompleteFalse;

                //Write information for the test to the console
                Console.WriteLine(string.Format(TestingFormat, "Packet Length", rtcpPacket.Length));

                //Write information for the test to the console
                Console.WriteLine(string.Format(TestingFormat, "Packet Padding", rtcpPacket.PaddingOctets));
            }

            //Create a new SendersReport with no blocks
            using (Media.Rtcp.RtcpReport testReport = new Media.Rtcp.SendersReport(2, false, 0, 7)) 
            {
                //The Media.RtcpData property contains all data which in the Media.RtcpPacket without padding
                if (testReport.RtcpData.Count() != 20 && testReport.Length != 20) throw invalidLength;

                output = testReport.Prepare().ToArray();//should be exactly equal to example
            }

            //Example of a Senders Report
            byte[] example = new byte[]
                         {
                            0x81,0xc8,0x00,0x0c,0xa3,0x36,0x84,0x36,0xd4,0xa6,0xaf,0x65,0x00,0x00,0x00,0x0,
                            0xcb,0xf9,0x44,0xd0,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xa3,0x36,0x84,0x36,
                            0x00,0xff,0xff,0xff,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                            0x00,0x00,0x00,0x00
                         };

             rtcpPacket= new Media.Rtcp.RtcpPacket(example, 0);
            if (rtcpPacket.Length != example.Length) throw new Exception("Invalid Length.");

            //Make a SendersReport to access the SendersInformation and ReportBlocks, do not dispose the packet when done with the report
            using (Media.Rtcp.SendersReport sr = new Media.Rtcp.SendersReport(rtcpPacket, false)) 
            {
                //Check the invalid block count
                if (sr.BlockCount != 16) throw new Exception("Invalid Block Count!");
                else Console.WriteLine(sr.BlockCount);//16, should be 1

                if ((uint)sr.SynchronizationSourceIdentifier != (uint)2738258998) throw new Exception("Invalid Senders SSRC!");
                else Console.WriteLine(sr.SynchronizationSourceIdentifier);//0xa3368436

                //Ensure setting the value through a setter is correct
                sr.NtpTimestamp = sr.NtpTimestamp;//14697854519044210688
                if ((ulong)sr.NtpTimestamp != 3567693669) throw new Exception("Invalid NtpTimestamp!");
                else Console.WriteLine(sr.NtpTimestamp);

                //Timestamp
                if ((uint)sr.RtpTimestamp != 3422110928) throw new Exception("Invalid RtpTimestamp!");
                else Console.WriteLine(sr.RtpTimestamp);//0

                //Data in report (Should only be 1)
                foreach (Media.Rtcp.IReportBlock rb in sr)
                {
                    if ((uint)rb.BlockIdentifier != 3567693669) throw new Exception("Invalid Source SSRC");
                    else if (rb is Media.Rtcp.ReportBlock)
                    {
                        Media.Rtcp.ReportBlock asReportBlock = (Media.Rtcp.ReportBlock)rb;

                        Console.WriteLine(asReportBlock.SendersSynchronizationSourceIdentifier);//0
                        Console.WriteLine(asReportBlock.FractionsLost);//0
                        Console.WriteLine(asReportBlock.CumulativePacketsLost);//0
                        Console.WriteLine(asReportBlock.ExtendedHighestSequenceNumberReceived);//0
                        Console.WriteLine(asReportBlock.InterarrivalJitterEstimate);//0
                        Console.WriteLine(asReportBlock.LastSendersReportTimestamp);//0
                    }
                }

                //Check the length to be exactly the same as the example 
                if (sr.Length != example.Length) throw new Exception("Invalid Length");

                //Verify SendersReport byte for byte
                output = sr.Prepare().ToArray();//should be exactly equal to example
                for (int i = 0, e = example.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");
            }

            if (rtcpPacket.Header.Disposed || rtcpPacket.Disposed) throw new Exception("Disposed the Media.RtcpPacket");
           
            //Now the packet can be disposed
            rtcpPacket.Dispose();
            rtcpPacket = null;

            //Next Sub Test
            /////

            using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7)) 
            {
                output = testReport.Prepare().ToArray();

                if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != ushort.MaxValue || testReport.Length != 8) throw new Exception("Invalid Length");

                if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");
            }

            

            //Add a Reason For Leaving

            using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7, Encoding.ASCII.GetBytes("v"))) 
            {
                output = testReport.Prepare().ToArray();

                if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != 2 || testReport.Length != 12) throw new Exception("Invalid Length");

                if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");
            }

            //Next Sub Test
            /////

            //Recievers Report and Source Description
            example = new byte[] { 0x81,0xc9,0x00,0x07,
                                   0x69,0xf2,0x79,0x50,
                                   0x61,0x37,0x94,0x50,
                                   0xff,0xff,0xff,0xff,
                                   0x00,0x01,0x00,0x52,
                                   0x00,0x00,0x0e,0xbb,
                                   0xce,0xd4,0xc8,0xf5,
                                   0x00,0x00,0x84,0x28,
                                   
                                   0x81,0xca,0x00,0x04,
                                   0x69,0xf2,0x79,0x50,
                                   0x01,0x06,0x4a,0x61,
                                   0x79,0x2d,0x50,0x43,
                                   0x00,0x00,0x00,0x00
            };


            //Could check for multiple packets with a function without having to keep track of the offset with the Media.RtcpPacket.GetPackets Function
            Media.Rtcp.RtcpPacket[] foundPackets = Media.Rtcp.RtcpPacket.GetPackets(example, 0, example.Length).ToArray();
            Console.WriteLine(foundPackets.Length);

            //Or manually for some reason
            rtcpPacket = new Media.Rtcp.RtcpPacket(example, 0); // The same as foundPackets[0]
            using (Media.Rtcp.ReceiversReport rr = new Media.Rtcp.ReceiversReport(rtcpPacket, false))
            {
                Console.WriteLine(rr.SynchronizationSourceIdentifier);//1777498448

                //Check the invalid block count
                if (rr.BlockCount != 16) throw new Exception("Invalid Block Count!");
                else Console.WriteLine(rr.BlockCount);//16, should be 1

                using (var enumerator = rr.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Console.WriteLine("Current IReportBlock Identifier: " + enumerator.Current.BlockIdentifier);//1631032400

                        //If the instance boxed in the Interface is a ReportBlock
                        if (enumerator.Current is Media.Rtcp.ReportBlock)
                        {
                            //Unbox the Interface as it's ReportBlock Instance
                            Media.Rtcp.ReportBlock asReportBlock = enumerator.Current as Media.Rtcp.ReportBlock;

                            Console.WriteLine("Found a ReportBlock");

                            //Print the instance information
                            Console.WriteLine("FractionsLost: " + asReportBlock.FractionsLost);//255/256 0xff
                            Console.WriteLine("CumulativePacketsLost: " + asReportBlock.CumulativePacketsLost);//-1, 0xff,0xff,0xff
                            Console.WriteLine("ExtendedHighestSequenceNumberReceived: " + asReportBlock.ExtendedHighestSequenceNumberReceived);//65618, 00, 01, 00, 52
                            Console.WriteLine("InterarrivalJitterEstimate: " + asReportBlock.InterarrivalJitterEstimate);//3771
                            Console.WriteLine("LastSendersReportTimestamp: " + asReportBlock.LastSendersReportTimestamp);//3470000128
                        }
                        else //Not a ReportBlock
                        {
                            Console.WriteLine("Current IReportBlock TypeName: " + enumerator.Current.GetType().Name);
                            Console.WriteLine("Current IReportBlock Data: " + BitConverter.ToString(enumerator.Current.BlockData.ToArray()));
                        }
                    }
                }

                //Verify RecieversReport byte for byte
                output = rr.Prepare().ToArray();//should be exactly equal to example
                for (int i = 0, e = rr.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            }

            if (rtcpPacket.Header.Disposed || rtcpPacket.Disposed) throw new Exception("Disposed the Media.RtcpPacket");

            //Now the packet can be disposed
            rtcpPacket.Dispose();
            rtcpPacket = null;

            //Make another packet instance from the rest of the example data.
            rtcpPacket = new Media.Rtcp.RtcpPacket(example, output.Length);

            //Create a SourceDescriptionReport from the packet instance to access the SourceDescriptionChunks
            using (Media.Rtcp.SourceDescriptionReport sourceDescription = new Media.Rtcp.SourceDescriptionReport(rtcpPacket, false)) 
            {

                foreach (var chunk in sourceDescription.GetChunkIterator())
                {
                    Console.WriteLine(string.Format(TestingFormat, "Chunk Identifier", chunk.ChunkIdentifer));
                    //Use a SourceDescriptionItemList to access the items within the Chunk
                    //This is performed auto magically when using the foreach pattern
                    foreach (Media.Rtcp.SourceDescriptionItem item in chunk /*.AsEnumerable<Rtcp.SourceDescriptionItem>()*/)
                    {
                        Console.WriteLine(string.Format(TestingFormat, "Item Type", item.ItemType));
                        Console.WriteLine(string.Format(TestingFormat, "Item Length", item.Length));
                        Console.WriteLine(string.Format(TestingFormat, "Item Data", BitConverter.ToString(item.Data.ToArray())));
                    }
                }

                //Verify SourceDescriptionReport byte for byte
                output = sourceDescription.Prepare().ToArray();//should be exactly equal to example
                for (int i = output.Length, e = sourceDescription.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            }



          

            //ApplicationSpecific - qtsi

            example = new byte[] { 0x81, 0xcc, 0x00, 0x06, 0x4e, 0xc8, 0x79, 0x50, 0x71, 0x74, 0x73, 0x69, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x61, 0x74, 0x00, 0x04, 0x00, 0x00, 0x00, 0x14 };

            rtcpPacket = new Media.Rtcp.RtcpPacket(example, 0);

            //Make a ApplicationSpecificReport instance
            Media.Rtcp.ApplicationSpecificReport app = new Media.Rtcp.ApplicationSpecificReport(rtcpPacket);
            
            //Check the name to be equal to qtsi
            if (!app.Name.SequenceEqual(Encoding.UTF8.GetBytes("qtsi"))) throw new Exception("Invalid App Packet Type");

            //Check the length
            if (rtcpPacket.Length != example.Length) throw new Exception("Invalid Legnth");

            //Verify ApplicationSpecificReport byte for byte
            output = rtcpPacket.Prepare().ToArray();//should be exactly equal to example
            for (int i = 0, e = example.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            //Test making a packet with a known length in bytes
            Media.Rtcp.SourceDescriptionReport sd = new Media.Rtcp.SourceDescriptionReport(2, false, 1, 0x0007);
            byte[] itemData = Encoding.UTF8.GetBytes("FLABIA-PC");
            sd.Add((Media.Rtcp.IReportBlock)new Media.Rtcp.SourceDescriptionChunk((int)0x1AB7C080, new Media.Rtcp.SourceDescriptionItem(Media.Rtcp.SourceDescriptionItem.SourceDescriptionItemType.CName, itemData.Length, itemData, 0))); // SSRC(4) ItemType(1), Length(1), ItemValue(9) = 15 Bytes
            rtcpPacket = sd; // Header = 4 Bytes in a SourceDescription, The First Chunk is `Overlapped` in the header.
            //asPacket now contains 11 octets in the payload.
            //asPacket now has 1 block (1 chunk of 15 bytes)
            //asPacket is 19 octets long, 11 octets in the payload and 8 octets in the header
            //asPacket would have a LengthInWordsMinusOne of 3 because 19 / 4 = 4 - 1 = 3
            //But null octets are added (Per RFC3550 @ Page 45 [Paragraph 2] / http://tools.ietf.org/html/rfc3550#appendix-A.4)
            //19 + 1 = 20, 20 / 4 = 5 - 1 = 4.
            if (!rtcpPacket.IsComplete || rtcpPacket.Length != 28 || rtcpPacket.Header.LengthInWordsMinusOne != 6) throw new Exception("Invalid Length");
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
                                foreach (Media.Rtcp.SourceDescriptionItem item in chunk /*.AsEnumerable<Rtcp.SourceDescriptionItem>()*/)
                                {
                                    Console.WriteLine(string.Format(TestingFormat, "Item Type", item.ItemType));
                                    Console.WriteLine(string.Format(TestingFormat, "Item Length", item.Length));
                                    Console.WriteLine(string.Format(TestingFormat, "Item Data", BitConverter.ToString(item.Data.ToArray())));
                                }
                            }
                        }
                        break;
                    }
                default: Console.WriteLine(Media.RtpTools.RtpSend.ToTextExpression(Media.RtpTools.FileFormat.Ascii, p)); break;

            }
        }

        static void TestRtpDumpReader(string path, Media.RtpTools.FileFormat? knownFormat = null)
        {
            //Always use an unknown format for the reader allows each item to be formatted differently
            using (Media.RtpTools.RtpDump.DumpReader reader = new Media.RtpTools.RtpDump.DumpReader(path))
            {

                Console.WriteLine(string.Format(TestingFormat, "Successfully Opened", path));

                while (reader.HasNext)
                {
                    Console.WriteLine(string.Format(TestingFormat, "ReaderPosition", reader.Position));

                    using (Media.RtpTools.RtpToolEntry entry = reader.ReadNext())
                    {

                        //Empty entry
                        if (entry.Length == 0)
                        {
                            //Found an empty entry
                            Console.WriteLine(string.Format(TestingFormat, "Found an Empty Entry", path));
                            continue;
                        }

                        //Check for the known format if given.
                        if (knownFormat.HasValue && reader.Format != knownFormat) throw new Exception("RtpDumpReader Format did not match knownFormat");

                        //Show the format of the item found
                        Console.WriteLine(string.Format(TestingFormat, "Format", entry.Format));


                        //Show the IP Address and Source in the entry, Note the entry has a TimevalSize property as well as a ReverseValues property which can change how the values are represented if needed.                        
                        Console.WriteLine(string.Format(TestingFormat, "Port", entry.Port));

                        Console.WriteLine(string.Format(TestingFormat, "Source", entry.Source));


                        //Additionally the Blob contains the RD_hdr_t and RD_packet_t but the Data property only will expose the octets required for the packets
                        byte[] data = entry.Data.ToArray();

                        int offset = 0, max = data.Length;

                        //Determine further action based on the PacketLength, Version etc.
                        if (entry.PacketLength == 0)
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
                            //Move the offrset
                            offset += Media.Rtp.RtpHeader.Length;
                            //If there are more bytes then that of the RtpHeader

                            if (offset < max) //Use the created packet so it can be disposed
                                using (Media.Rtp.RtpPacket p = new Media.Rtp.RtpPacket(header, new ArraySegment<byte>(data, offset, max - offset), false))
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

        static System.Net.IPEndPoint testingEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7);

        /// <summary>
        /// Creates a <see cref="DumpWriter"/> and writes a single <see cref="Rtp.RtpPacket"/>, and a single <see cref="Rtcp.RtcpPacket"/>.
        /// </summary>
        /// <param name="path">The path to write the packets to</param>
        /// <param name="format">The format the packets should be written in</param>
        static void TestRtpDumpWriter(string path, Media.RtpTools.FileFormat format)
        {
            //Use a write to write a RtpPacket
            using (Media.RtpTools.RtpDump.DumpWriter dumpWriter = new Media.RtpTools.RtpDump.DumpWriter(path, Media.RtpTools.FileFormat.Header, testingEndPoint))
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

            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("RtpDump Test - " + currentPath);

            #region Test Reader with Unknown format on example file with expected format

            //Should find Media.RtpTools.FileFormat.Binary
            TestRtpDumpReader(currentPath + @"\bark.rtp", Media.RtpTools.FileFormat.Binary);

            //TestRtpDumpReader(currentPath + @"\video.rtpdump", Media.RtpTools.FileFormat.Binary);

            #endregion

            #region Test Writer on various formats

            TestRtpDumpWriter(currentPath + @"\BinaryDump.rtpdump", Media.RtpTools.FileFormat.Binary);

            TestRtpDumpWriter(currentPath + @"\Header.rtpdump", Media.RtpTools.FileFormat.Header);

            TestRtpDumpWriter(currentPath + @"\AsciiDump.rtpdump", Media.RtpTools.FileFormat.Ascii);

            TestRtpDumpWriter(currentPath + @"\HexDump.rtpdump", Media.RtpTools.FileFormat.Hex);

            TestRtpDumpWriter(currentPath + @"\ShortDump.rtpdump", Media.RtpTools.FileFormat.Short);

            #endregion

            #region Test Reader on those expected formats

            TestRtpDumpReader(currentPath + @"\BinaryDump.rtpdump", Media.RtpTools.FileFormat.Binary);

            TestRtpDumpReader(currentPath + @"\Header.rtpdump", Media.RtpTools.FileFormat.Header);

            TestRtpDumpReader(currentPath + @"\AsciiDump.rtpdump", Media.RtpTools.FileFormat.Ascii);

            TestRtpDumpReader(currentPath + @"\HexDump.rtpdump", Media.RtpTools.FileFormat.Text);

            TestRtpDumpReader(currentPath + @"\ShortDump.rtpdump", Media.RtpTools.FileFormat.Short);

            #endregion

            #region Read Example rtpdump file 'bark.rtp' and Write the same file as 'mybark.rtp'

            //Maintain a count of how many packets were written for next test
            int writeCount;

            using (Media.RtpTools.RtpDump.DumpReader reader = new Media.RtpTools.RtpDump.DumpReader(currentPath + @"\bark.rtp"))
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

                            //Check for Media.RtcpPackets first
                            if (entry.PacketLength == 0)
                            {
                                //Reading compound packets out of a single item
                                foreach (Media.Rtcp.RtcpPacket rtcpPacket in Media.Rtcp.RtcpPacket.GetPackets(entry.Blob, 0, entry.Blob.Length))
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
                                Media.Rtp.RtpPacket rtpPacket = new Media.Rtp.RtpPacket(entry.Blob, 0);
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

        //Declare a few exceptions
        static Exception versionException = new Exception("Unable to set the version"),
            extensionException = new Exception("Incorrectly set the Extensions Bit"),
            contributingSourceException = new Exception("Incorrectly set the ContributingSource nibble"),
            inValidHeaderException = new Exception("Invalid header."),
            reportBlockException = new Exception("Incorrectly set the ReportBlock 7 bits"),
            paddingException = new Exception("Incorreclty set the Padding Bit"),
            timestampException = new Exception("Incorrect Timestamp value"),
            sequenceNumberException = new Exception("Sequence Number Incorrect"),
            markerException = new Exception("Marker is not set"),
            payloadException = new Exception("Incorreclty set PayloadType bits");

        static void TraceMessage(string message,
        string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine(string.Format(TestingFormat, message, memberName ?? "No MethodName Provided"));
        }
        static void TestRtpPacket()
        {
            //Create a RtpPacket instance
            Media.Rtp.RtpPacket p = new Media.Rtp.RtpPacket(new Media.Rtp.RtpHeader(0, false, false), Enumerable.Empty<byte>());

            //Set a few values
            p.Timestamp = 987654321;
            p.SequenceNumber = 7;
            p.ContributingSourceCount = 7;
            if (p.SequenceNumber != 7) throw sequenceNumberException;

            if (p.Timestamp != 987654321) throw timestampException;

            if (p.ContributingSourceCount != 7) throw contributingSourceException;

            //Recreate the packet from the bytes of the result of calling the methods ToArray on the Prepare instance method.
            p = new Media.Rtp.RtpPacket(p.Prepare().ToArray(), 0);

            //Perform the same tests. (Todo condense tests into seperate functions)

            if (p.SequenceNumber != 7) throw sequenceNumberException;

            if (p.ContributingSourceCount != 7) throw contributingSourceException;

            if (p.Timestamp != 987654321) throw timestampException;

            //Cache a bitValue
            bool bitValue = false;

            //Permute every possible bit packed value that can be valid in the first and second octet
            for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
            {
                //Make a bitValue after the 0th iteration
                if (ibitValue > 0) bitValue = Convert.ToBoolean(bitValue);

                //Complete tested the first and second octets with the current bitValue
                if (ibitValue <= 1) Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

                //Permute every possible value within the 2 bit Version
                for (int VersionCounter = 0; VersionCounter < 4; ++VersionCounter)
                {
                    //Set the version
                    p.Version = VersionCounter;

                    //Write the version information to the console.
                    Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                    Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

                    //Set the bit values in the first octet
                    p.Extension = p.Padding = bitValue;

                    //Check the version bits after modification
                    if (p.Version != VersionCounter) throw versionException;

                    //Check the Padding bit after modification
                    if (p.Padding != bitValue) throw paddingException;

                    //Check the Extension bit after modification
                    if (p.Extension != bitValue) throw extensionException;

                    //Permute every possible value in the 7 bit PayloadCounter
                    for (int PayloadCounter = 0; PayloadCounter <= sbyte.MaxValue; ++PayloadCounter)
                    {
                        //Set the 7 bit value in the second octet.
                        p.PayloadType = (byte)PayloadCounter;

                        //Write the value of the PayloadCounter to the console and the packet value to the Console.
                        Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                        Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

                        //Check the PayloadType
                        if (p.PayloadType != PayloadCounter) throw payloadException;

                        //Check the Padding bit after setting the PayloadType
                        if (p.Padding != bitValue) throw paddingException;

                        //Check the Extensions bit after setting the PayloadType
                        if (p.Extension != bitValue) throw extensionException;

                        //Permute every combination for a nybble
                        for (int ContributingSourceCounter = byte.MinValue; ContributingSourceCounter <= 15; ++ContributingSourceCounter)
                        {
                            ///////////////Set the CC nibble in the first Octet
                            p.ContributingSourceCount = (byte)ContributingSourceCounter;
                            /////////////

                            //Identify the Contributing Source Counter and the Packet's value
                            Console.Write(string.Format(TestingFormat, "\tContributingSourceCounter", ContributingSourceCounter));
                            Console.Write(string.Format(TestingFormat, " ContributingSourceCount", p.ContributingSourceCount + "\r\n"));

                            //Check the CC nibble in the first octet.
                            if (p.ContributingSourceCount != ContributingSourceCounter) throw contributingSourceException;

                            //Ensure the Version after modification
                            if (p.Version != VersionCounter) throw versionException;

                            //Check the Padding after modification
                            if (p.Padding != bitValue) throw paddingException;

                            //Check the Extensions after modification
                            if (p.Extension != bitValue) throw extensionException;

                            ///////////////Serialize the packet
                            p = new Media.Rtp.RtpPacket(p.Prepare().ToArray(), 0);
                            /////////////

                            //Ensure the version remains after modification
                            if (p.Version != VersionCounter) throw versionException;

                            //Ensure the Padding bit after modification
                            if (p.Padding != bitValue) throw paddingException;

                            //Ensure the Extension bit after modification
                            if (p.Extension != bitValue) throw extensionException;

                            //Ensure the ContributingSourceCount after modification
                            if (p.ContributingSourceCount != ContributingSourceCounter) throw contributingSourceException;
                        }
                    }
                }                
                Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
            }

        }

        static void TestRtpExtension()
        {
            //Test TestOnvifRTPExtensionHeader (Thanks to Wuffles@codeplex)

            byte[] m_SamplePacketBytes = new byte[]
                                      {
                                          0x90, 0x60, 0x94, 0x63, // RTP Header
                                          0x0D, 0x19, 0x60, 0xC9, // .
                                          0xA6, 0x20, 0x13, 0x44, // .

                                          0xAB, 0xAC, 0x00, 0x03, // Extension Header   
                                          0xD4, 0xBB, 0x8A, 0x43, // Extension Data     
                                          0xFE, 0x7A, 0xC8, 0x1E, // Extension Data     
                                          0x00, 0xD3, 0x00, 0x00, // Extension Data     

                                          0x5C, 0x81, 0x9B, 0xC0, // RTP Payload start
                                          0x1C, 0x02, 0x38, 0x8E, // .
                                          0x2B, 0xC0, 0x01, 0x09, // .
                                          0x55, 0x77, 0x49, 0x99, // .
                                          0x62, 0xFF, 0xBA, 0xC9, // .
                                          0x8E, 0xCE, 0x23, 0x96, // .
                                          0x6A, 0xCC, 0xF5, 0x5F, // .
                                          0xA0, 0x08, 0xD9, 0x37, // .
                                          0xCF, 0xFA, 0xA5, 0x4D, // .
                                          0x16, 0x6C, 0x78, 0x61, // .
                                          0xFA, 0x7F, 0xC8, 0x7E, // .
                                          0xA1, 0x15, 0xF6, 0x5F, // .
                                          0xA3, 0x2F, 0x82, 0xC7, // .
                                          0x45, 0x0A, 0x87, 0x75, // .
                                          0xEC, 0x5B, 0x7D, 0xDE, // .
                                          0x82, 0x31, 0xD0, 0xE9, // .
                                          0xBE, 0xE5, 0x39, 0x8D, // etc. 
                                      };

            Media.Rtp.RtpPacket testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

            if (testPacket.Extension)
            {
                using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
                {
                    // The extension data length is (3 words / 12 bytes) 
                    // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                    //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                    if (rtpExtension.Size != 16) throw new Exception("Expected ExtensionLength not found");
                    else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                    // Check extension values are what we expected.
                    if (rtpExtension.Flags != 0xABAC) throw new Exception("Expected Extension Flags Not Found");
                    else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                    // Test the extension data is correct
                    byte[] expected = { 0xD4, 0xBB, 0x8A, 0x43, 0xFE, 0x7A, 0xC8, 0x1E, 0x00, 0xD3, 0x00, 0x00 };
                    if (!rtpExtension.Data.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
                    else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
                }
            }

            byte[] output = testPacket.Prepare().ToArray();

            if (output.Length != m_SamplePacketBytes.Length) throw new Exception("Packet was not the same");

            for (int i = 0, e = testPacket.Length; i < e; ++i) if (output[i] != m_SamplePacketBytes[i]) throw new Exception("Packet was not the same");


            Console.WriteLine();

            m_SamplePacketBytes = new byte[]
                                      {
                                          //RTP Header
                                             0x90,0x1a,0x01,0x6d,0xf3,0xff,0x40,0x58,0xf0,0x00,0x9c,0x5b,
                                             //Extension FF FF, Length = 0
                                             0xff,0xff,0x00,0x00

                                            ,0x00,0x00,0x15,0x1c,0x01,0xff,0xa0,0x5a,0x13,0xd2,0xa9,0xf5,0xeb,0x49,0x52,0xdb
                                            ,0x65,0xa8,0xd8,0x40,0x36,0xa8,0x03,0x81,0x41,0xf7,0xa5,0x34,0x52,0x28,0x4f,0x7a
                                            ,0x29,0x79,0xf5,0xa4,0xa0,0x02,0x8a,0x5a,0x4a,0x00,0x28,0xed,0x45,0x14,0x0c,0x28
                                            ,0xef,0x45,0x1c,0xd0,0x20,0xa3,0x34,0x51,0x40,0xc4,0xcd,0x2d,0x14,0x7e,0x34,0x00
                                            ,0x7f,0x5a,0x4a,0x5a,0x29,0x00,0x94,0x52,0xd0,0x72,0x69,0x88,0x4e,0x68,0xa3,0x9a
                                            ,0x5a,0x43,0x12,0x8a,0x39,0xa0,0x73,0x4c,0x02,0x8a,0x5a,0x29,0x00,0x63,0x8e,0x28
                                            ,0xa5,0xcf,0x14,0x94,0x00,0x51,0x45,0x14,0x08,0x4a,0x51,0x9a,0x5c,0x52,0x00,0x68
                                            ,0x01,0x40,0xa4,0xa7,0x04,0x62,0x78,0xa9,0x52,0x06,0x6e,0xd4,0x01,0x12,0x8e,0x7d
                                            ,0x69,0x36,0x92,0x71,0x8a,0xd0,0x83,0x4f,0x91,0xfa,0x29,0x35,0x7a,0x1d,0x25,0xb3
                                            ,0xf3,0xfc,0xa3,0xde,0x9d,0x85,0x73,0x11,0x62,0x63,0x56,0x63,0xb3,0x76,0xc6,0x14
                                            ,0xd6,0xc9,0x86,0xca,0xd8,0x66,0x49,0x41,0x3e,0x82,0xaa,0x4f,0xad,0xdb,0xdb,0xf1
                                            ,0x12,0x8e,0x3b,0x9a,0x05,0x71,0xb1,0x69,0x72,0x1e,0x48,0xc0,0xf7,0xa9,0x8d,0xa5
                                            ,0xbc,0x03,0x32,0x48,0x3f,0x0a,0xc4,0xbd,0xf1,0x2c,0x8f,0x90,0x1f,0x03,0xd0,0x56
                                            ,0x35,0xce,0xaf,0x2c,0x87,0xef,0x53,0xb0,0x1d,0x54,0xd7,0xf6,0xb0,0x70,0x80,0x1f
                                            ,0x73,0x59,0xb7,0x9a,0xde,0x54,0x85,0x60,0x3e,0x95,0xcc,0x4d,0x79,0x23,0xf5,0x63
                                            ,0x55,0xda,0x56,0x27,0x92,0x69,0xf2,0xdc,0x2e,0x5f,0x97,0x54,0x91,0xdc,0xe5,0x89
                                            ,0xaa,0x57,0x77,0x4e,0xea,0x70,0x6a,0x0e,0x73,0x9a,0x8d,0xbe,0xb5,0x56,0x44,0xb6
                                            ,0x46,0x8e,0x59,0xdb,0x2d,0x9c,0x76,0xf4,0xa7,0xfe,0x95,0x1a,0x9c,0x12,0x79,0xa7
                                            ,0x82,0x4d,0x50,0x8f,0x41,0xa0,0x52,0xd2,0x71,0x59,0x1a,0x00,0x34,0xef,0x73,0x49
                                            ,0x9a,0x3e,0xa6,0x80,0x0a,0x28,0xe2,0x8e,0xf4,0x00,0x01,0x4b,0x41,0x22,0x90,0x30
                                            ,0xcf,0x51,0x45,0xc4,0x07,0x9a,0x51,0xd6,0x93,0x7a,0xff,0x00,0x78,0x7e,0x74,0x82
                                            ,0x44,0x1d,0x58,0x50,0x31,0xc3,0x39,0x39,0xa3,0x34,0x82,0x58,0xc7,0xf1,0x8f,0xce
                                            ,0x93,0xcf,0x8b,0xfb,0xe2,0x8b,0x88,0x7d,0x2e,0x0d,0x45,0xf6,0x88,0xfb,0xb8,0xfc
                                            ,0xe8,0xfb,0x54,0x3f,0xf3,0xd0,0x51,0x70,0x25,0xa2,0xa1,0xfb,0x5c,0x3d,0xdc,0x50
                                            ,0x2e,0xe1,0xcf,0xdf,0x14,0x5c,0x64,0xe0,0x51,0xc8,0xa4,0x57,0x57,0xe4,0x10,0x69
                                            ,0xc7,0x91,0x40,0x84,0xa5,0xa0,0x76,0x34,0x50,0x01,0x8c,0x9a,0x29,0x7a,0x52,0xfb
                                            ,0xd0,0x03,0x40,0xce,0x73,0x4b,0xb7,0x8a,0x5e,0x94,0x84,0xd0,0x03,0x31,0xcd,0x14
                                            ,0xe3,0x49,0x8c,0xd0,0x31,0x41,0xa7,0x0f,0x5a,0x8f,0xbd,0x3d,0x78,0x34,0x08,0x7f
                                            ,0x5e,0xb4,0x62,0x8c,0x64,0xd2,0xe3,0xd6,0x80,0x12,0x81,0x4b,0x8c,0xfb,0x51,0xc5
                                            ,0x00,0x14,0x87,0x91,0x4a,0x4d,0x1e,0xbc,0xd0,0x21,0xb9,0xcd,0x3b,0xde,0x8f,0xad
                                            ,0x28,0xa0,0x05,0xf4,0xa3,0xa9,0xa3,0x14,0x94,0x80,0x05,0x07,0xda,0x8a,0x5a,0x60
                                            ,0x27,0x7a,0x3b,0xe6,0x96,0x8e,0xb4,0x80,0x4e,0xb4,0x84,0x77,0xa5,0xe4,0x52,0x1a
                                            ,0x00,0x6e,0xda,0x8c,0x8e,0xb5,0x2d,0x30,0xe7,0xad,0x03,0x18,0x6a,0x39,0x33,0x52
                                            ,0x53,0x25,0xe9,0xd6,0x81,0x94,0xe5,0xe9,0x55,0x24,0xfb,0xc3,0xeb,0x56,0xe4,0xe9
                                            ,0x55,0x1b,0xef,0x0a,0x68,0x0b,0x30,0xf2,0xcb,0xf5,0xae,0x93,0x53,0xe3,0x4a,0x1f
                                            ,0x85,0x73,0x90,0x0f,0xde,0x2f,0xd6,0xba,0x4d,0x57,0x8d,0x2c,0x1f,0xa5,0x0f,0x63
                                            ,0x2a,0xbf,0x09,0x8d,0x6b,0x7d,0x2d,0xba,0xed,0x5c,0x11,0x5a,0xb6,0x9a,0xcb,0xa9
                                            ,0x1b,0x93,0x35,0x84,0xbe,0xb5,0x62,0x2e,0xbc,0x57,0x34,0x99,0xca,0xa7,0x25,0xb1
                                            ,0xdb,0xd8,0x6b,0xaa,0x17,0xee,0x10,0xd5,0xbb,0xa3,0x6a,0xcc,0x26,0xcc,0x8d,0x95
                                            ,0x6e,0xa2,0xbc,0xfe,0xd1,0xb6,0xe3,0x9a,0xdc,0xb2,0x9f,0x18,0xe6,0xb8,0xaa,0x26
                                            ,0x3f,0xac,0x4e,0xeb,0x53,0xd3,0x81,0x59,0x10,0x11,0x82,0xa6,0xa1,0x91,0x30,0xd8
                                            ,0x27,0x22,0xa9,0xf8,0x7e,0x7f,0x36,0xc8,0x6e,0x39,0xc1,0xa4,0xbe,0xbb,0x09,0x70
                                            ,0x57,0x3d,0x2a,0x66,0xd3,0x8a,0xee,0x74,0xd6,0xd6,0x0a,0x45,0xcf,0x2d,0x4d,0x21
                                            ,0x88,0x55,0x05,0xbd,0x1e,0xb5,0x28,0xbc,0x07,0xbd,0x61,0x76,0x71,0x68,0x5b,0xfb
                                            ,0x28,0x23,0x34,0xf8,0x20,0x11,0x12,0x7d,0x6a,0x18,0x2e,0xd4,0xf0,0xc6,0xae,0x03
                                            ,0x9e,0x6b,0xb7,0x0f,0x0a,0x73,0xdd,0xea,0x8e,0xca,0x50,0x83,0xf7,0x96,0xe4,0x77
                                            ,0x11,0xf9,0xb1,0x32,0xd6,0x4c,0x96,0x27,0xba,0x9a,0xda,0xa6,0x4b,0x22,0xc6,0x3e
                                            ,0x63,0xf8,0x56,0x98,0x9a,0x51,0x4d,0xce,0xe3,0xad,0x4a,0x32,0xd5,0xb3,0x9e,0x96
                                            ,0xcf,0xda,0xa8,0xdc,0x59,0xe3,0x27,0x15,0xd0,0x3b,0x2b,0x12,0x6a,0xad,0xc0,0x52
                                            ,0x0d,0x79,0xe9,0x9e,0x7b,0x8a,0x28,0xe9,0xb6,0xf8,0xb5,0x6e,0x3b,0xd5,0x5b,0xc4
                                            ,0xc6,0x45,0x6e,0x59,0x28,0xfb,0x23,0x1f,0x7a,0xc8,0xd4,0x78,0x26,0xbb,0xe9,0x3b
                                            ,0xa3,0xd8,0xa4,0xad,0x49,0x18,0x53,0x8e,0x6b,0x1b,0x59,0x1f,0xe8,0xad,0x5b,0x33
                                            ,0x9e,0x6b,0x1b,0x59,0x39,0xb5,0x35,0xd9,0xd0,0xc6,0xaf,0xc2,0xca,0x9e,0x1d,0x1c
                                            ,0xc9,0xf5,0xab,0x77,0x03,0x13,0xb7,0xd7,0xad,0x56,0xf0,0xef,0x59,0x3e,0xb5,0x6e
                                            ,0xe0,0x66,0x67,0xf5,0xcd,0x69,0x1d,0x8c,0xa1,0xb2,0x22,0x23,0xde,0x9b,0x4e,0xa3
                                            ,0x03,0xad,0x05,0x89,0x40,0xcf,0x7a,0x5e,0xd9,0xa4,0xef,0x40,0x06,0x38,0x34,0x63
                                            ,0x1e,0xf4,0x1e,0x07,0xd6,0x8f,0x4a,0x63,0x0f,0xeb,0x48,0x07,0xad,0x3b,0x81,0x4b
                                            ,0xd7,0x14,0x80,0x6f,0x4c,0xe6,0x93,0x8c,0x75,0xa5,0x23,0xae,0x68,0xc7,0x38,0xa6
                                            ,0x21,0x31,0xda,0x93,0x18,0xa7,0xe0,0x77,0x34,0x98,0xcd,0x00,0x34,0xf7,0xed,0x49
                                            ,0x8a,0x7e,0x33,0x48,0x45,0x00,0x34,0x77,0xa3,0x93,0x4b,0x4a,0x7d,0x28,0x01,0x33
                                            ,0x8a,0x30,0x71,0xcf,0x39,0xa2,0x97,0x1e,0xd4,0x00,0x2f,0xd2,0x9e,0x29,0x00,0xa7
                                            ,0x7b,0x66,0x81,0x09,0x8f,0x6a,0x43,0x9c,0x1e,0x29,0xe4,0x66,0x9a,0x72,0x38,0xa4
                                            ,0x52,0x2b,0xc8,0x0f,0x3d,0xaa,0xb4,0xb9,0x1f,0x4a,0xb5,0x27,0x7a,0xad,0x21,0xea
                                            ,0x2a,0x59,0x48,0xa8,0xfe,0xb5,0x03,0x70,0x6a,0xc4,0x83,0x15,0x5c,0xf5,0xa0,0xa1
                                            ,0x56,0xb6,0x3c,0x3d,0xff,0x00,0x1f,0xa9,0x58,0xe3,0xde,0xb6,0x7c,0x3d,0xff,0x00
                                            ,0x1f,0x89,0x42,0x14,0xb6,0x3b,0x1a,0x43,0x41,0x6e,0x29,0x33,0x56,0x66,0x14,0xbf
                                            ,0x53,0x4d,0xcd,0x26,0xf1,0xeb,0x40,0x0e,0x34,0xdf,0xad,0x30,0xca,0xa3,0xa9,0xa8
                                            ,0x24,0xbc,0x8d,0x7a,0x9a,0x57,0x0b,0x12,0xc9,0x55,0x49,0x1d,0xcd,0x55,0xbb,0xd5
                                            ,0x02,0xa9,0xd8,0xb9,0xfa,0xd6,0x3c,0xd7,0xf3,0x4d,0x9c,0xb6,0x07,0xa0,0xa3,0x98
                                            ,0x7c,0xac,0xda,0x9a,0xea,0x18,0xb3,0xb9,0xc7,0xe1,0x59,0xd3,0xea,0xc3,0x27,0xca
                                            ,0x4e,0x7d,0x4d,0x66,0x33,0x13,0xc9,0x24,0xd3,0x0f,0xeb,0x4b,0x99,0xb1,0xa8,0xa2
                                            ,0x69,0xee,0xe7,0x9b,0xef,0x39,0xc7,0xa0,0xaa,0xc7,0x9e,0xf4,0xa6,0x9a,0x69,0x17
                                            ,0x61,0x0d,0x25,0x2f,0xd6,0x83,0x40,0xc6,0x9a,0x28,0xeb,0x41,0x14,0x00,0x94,0x1a
                                            ,0x28,0xa0,0x04,0xf7,0xa5,0xa2,0x92,0x90,0x05,0x1d,0x28,0xa5,0xa0,0x62,0x73,0x45
                                            ,0x14,0x62,0x80,0x0a,0x3d,0x68,0xa2,0x80,0x0a,0x28,0xc5,0x2d,0x30,0x0e,0x94,0x7d
                                            ,0x69,0x69,0x29,0x00,0x94,0xb8,0xcf,0x5a,0x31,0x4b,0xed,0x40,0x08,0x0d,0x18,0xe2
                                            ,0x8a,0x28,0x10,0x9c,0xd1,0x4b,0x46,0x28,0x01,0x39,0xcd,0x14,0x51,0x8a,0x00,0x31
                                            ,0x49,0x9e,0x69,0xe4,0x52,0x00,0x73,0xcd,0x03,0x12,0x8a,0x90,0x46,0xc7,0xb5,0x4f
                                            ,0x1d,0xa3,0xb9,0xc0,0x52,0x68,0x11,0x57,0x06,0x9c,0xa8,0xc6,0xb5,0xe0,0xd1,0xe5
                                            ,0x6e,0x59,0x71,0xf5,0xab,0x8b,0x63,0x69,0x6e,0x33,0x34,0xcb,0xc7,0x61,0x4e,0xc2
                                            ,0xb9,0x84,0x96,0xce,0xc7,0xa1,0xab,0x90,0x69,0x72,0xbf,0x45,0x38,0xab,0xd2,0x6a
                                            ,0x76,0x36,0xdc,0x46,0x81,0x8f,0xa9,0xac,0xeb,0xbf,0x11,0xb6,0x08,0x56,0x0a,0x3d
                                            ,0xa9,0x0b,0x53,0x4a,0x3d,0x29,0x13,0xfd,0x6b,0xaa,0x8f,0xad,0x2b,0xcf,0xa7,0xda
                                            ,0xf7,0xde,0xc2,0xb9,0x1b,0x9d,0x71,0xdf,0x3f,0x39,0x3f,0x8d,0x66,0x4f,0xa8,0xca
                                            ,0xfd,0x0d,0x52,0xb8,0x1d,0xa5,0xc7,0x88,0xd2,0x20,0x44,0x4a,0xab,0x58,0x97,0x7e
                                            ,0x23,0x95,0xc9,0xc3,0x93,0x5c,0xdb,0xca,0xef,0xc9,0x63,0x4c,0xc9,0x34,0xf9,0x3b
                                            ,0x8a,0xe8,0xd0,0xb8,0xd5,0x66,0x94,0x9c,0xb1,0xaa,0x6f,0x71,0x23,0xe7,0x2c,0x6a
                                      };

            testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

            if (testPacket.Extension)
            {
                using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
                {
                    // The extension data length is (3 words / 12 bytes) 
                    // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                    //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                    if (rtpExtension.Size != 4) throw new Exception("Expected ExtensionLength not found");
                    else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                    // Check extension values are what we expected.
                    if (rtpExtension.Flags != 0xFFFF) throw new Exception("Expected Extension Flags Not Found");
                    else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                    // Test the extension data is correct
                    byte[] expected = Utility.Empty;
                    if (!rtpExtension.Data.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
                    else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
                }
            }

        }


  

        private static void TestRtcpPacket()
        {
            //Write all Abstrractions to the console
            foreach (var abstraction in Media.Rtcp.RtcpPacket.GetImplementedAbstractions())
                Console.WriteLine(string.Format(TestingFormat, "\tFound Abstraction" ,"Implemented By" + abstraction.Name));

            //Write all Implementations to the console
            foreach (var implementation in Media.Rtcp.RtcpPacket.GetImplementations())
                Console.WriteLine(string.Format(TestingFormat, "\tPayloadType " + implementation.Key, "Implemented By" + implementation.Value.Name));

            //Create a RtpPacket instance
            Media.Rtcp.RtcpPacket p = new Media.Rtcp.RtcpPacket(new Media.Rtcp.RtcpHeader(0, 0, false, 0), Enumerable.Empty<byte>());

            //Set a values
            p.SynchronizationSourceIdentifier = 7;

            if (p.SynchronizationSourceIdentifier != 7) throw sequenceNumberException;

            //Cache a bitValue
            bool bitValue = false;

            //Test every possible bit packed value that can be valid in the first and second octet
            for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
            {
                //Make a bitValue after the 0th iteration
                if (ibitValue > 0) bitValue = Convert.ToBoolean(ibitValue);

                //Complete tested the first and second octets with the current bitValue
                Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

                //Permute every possible value within the 2 bit Version
                for (int VersionCounter = 0; VersionCounter < 4; ++VersionCounter)
                {
                    //Set the version
                    p.Version = VersionCounter;

                    //Write the version information to the console.
                    Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                    Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

                    //Set the bit values in the first octet
                    p.Padding = bitValue;

                    //Check the version bits after modification
                    if (p.Version != VersionCounter) throw versionException;

                    //Check the Padding bit after modification
                    if (p.Padding != bitValue) throw paddingException;

                    //Permute every possible value in the 7 bit PayloadCounter
                    for (int PayloadCounter = 0; PayloadCounter <= byte.MaxValue; ++PayloadCounter)
                    {
                        //Set the 7 bit value in the second octet.
                        p.PayloadType = (byte)PayloadCounter;

                        //Write the value of the PayloadCounter to the console and the packet value to the Console.
                        Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                        Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

                        //Check the PayloadType
                        if (p.PayloadType != PayloadCounter) throw payloadException;

                        //Check the Padding bit after setting the PayloadType
                        if (p.Padding != bitValue) throw paddingException;

                        //Permute every combination for a nybble
                        for (int ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
                        {
                            ///////////////Set the CC nibble in the first Octet
                            p.BlockCount = (byte)ReportBlockCounter;
                            /////////////                            

                            //Identify the Contributing Source Counter and the Packet's value
                            Console.Write(string.Format(TestingFormat, "\tReportBlockCounter", ReportBlockCounter));
                            Console.Write(string.Format(TestingFormat, " BlockCount", p.BlockCount + "\r\n"));

                            //Check the BlockCount
                            if (p.BlockCount != ReportBlockCounter) throw reportBlockException;

                            //Ensure the Version after modification
                            if (p.Version != VersionCounter) throw versionException;

                            //Check the Padding after modification
                            if (p.Padding != bitValue) throw paddingException;

                            ///////////////Serialize the packet
                            p = new Media.Rtcp.RtcpPacket(p.Prepare().ToArray(), 0);
                            /////////////

                            //Ensure the version remains after modification
                            if (p.Version != VersionCounter) throw versionException;

                            //Ensure the Padding bit after modification
                            if (p.Padding != bitValue) throw paddingException;

                            //Check the BlockCount after modification
                            if (p.BlockCount != ReportBlockCounter) throw reportBlockException;

                            //Check for a valid header
                            if (!p.Header.IsValid(VersionCounter, PayloadCounter, bitValue)
                                || //Check for validation per RFC3550 A.1 when the test permits
                                !bitValue && VersionCounter > 1 && PayloadCounter >= 200 && PayloadCounter <= 201 && !RFC3550.IsValidRtcpHeader(p.Header, VersionCounter)) throw inValidHeaderException;

                            //Perform checks with length in words set incorrectly
                        }
                    }
                }                
                Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
            }          
        }
        

        static void TestRtspMessage()
        {
            Media.Rtsp.RtspMessage request = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request);
            request.Location = new Uri("rtsp://someServer.com");
            request.Method = Media.Rtsp.RtspMethod.REDIRECT;
            request.Version = 7;
            request.CSeq = 2;
            
            byte[] bytes = request.ToBytes();

            Media.Rtsp.RtspMessage fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.Method == request.Method && fromBytes.Location == request.Location && fromBytes.CSeq == request.CSeq))
            {
                throw new Exception("Request Testing Failed!");
            }

            Media.Rtsp.RtspMessage response = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response)
            {
                Version = 7
            };

            response.StatusCode = Media.Rtsp.RtspStatusCode.SeeOther;
            response.CSeq = fromBytes.CSeq;
            response.Version = fromBytes.Version;

            bytes = response.ToBytes();

            fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.StatusCode == response.StatusCode && fromBytes.CSeq == request.CSeq))
            {
                throw new Exception("Response Testing Failed!");
            }

            //Check without Headers.

            request = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request);
            request.Location = new Uri("rtsp://someServer.com");
            request.Method = Media.Rtsp.RtspMethod.REDIRECT;
            request.Version = 7;
            
            bytes = request.ToBytes();

            fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.Method == request.Method && fromBytes.Location == request.Location && fromBytes.Version == response.Version))
            {
                throw new Exception("Request Testing Failed!");
            }

            response = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response)
            {
                Version = 7
            };

            response.StatusCode = Media.Rtsp.RtspStatusCode.SeeOther;
            response.Version = fromBytes.Version;

            bytes = response.ToBytes();

            fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.StatusCode == response.StatusCode && fromBytes.Version == response.Version))
            {
                throw new Exception("Response Testing Failed!");
            }

            //Check With Body.

            request = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Request);
            request.Location = new Uri("rtsp://someServer.com");
            request.Method = Media.Rtsp.RtspMethod.REDIRECT;
            request.Version = 7;
            request.Body = "Testing";

            bytes = request.ToBytes();

            fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.Method == request.Method && fromBytes.Location == request.Location && fromBytes.Version == response.Version && fromBytes.Body == "Testing\r\n"))
            {
                throw new Exception("Request Testing Failed!");
            }

            response = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response)
            {
                Version = 7
            };

            response.StatusCode = Media.Rtsp.RtspStatusCode.SeeOther;
            response.Version = fromBytes.Version;

            bytes = response.ToBytes();
            
            fromBytes = new Media.Rtsp.RtspMessage(bytes);

            if (!(fromBytes.StatusCode == response.StatusCode && fromBytes.Version == response.Version && fromBytes.Body == response.Body))
            {
                throw new Exception("Response Testing Failed!");
            }

            //Test Parsing bytes containing valid and invalid messages

        }

        static void TryPrintPacket(bool incomingFlag, Media.Common.IPacket packet, bool writePayload = false) { TryPrintClientPacket(null, incomingFlag, packet, writePayload); }

        static void TryPrintClientPacket (object sender,  bool incomingFlag, Media.Common.IPacket packet, bool writePayload = false)
        {
            if (sender is Media.Rtp.RtpClient && (sender as Media.Rtp.RtpClient).Disposed) return;            

            ConsoleColor previousForegroundColor = Console.ForegroundColor,
                    previousBackgroundColor = Console.BackgroundColor;

            string format = "{0} a {1} {2}";

            Type packetType = packet.GetType();

            if (packet is Media.Rtp.RtpPacket)
            {
                Media.Rtp.RtpPacket rtpPacket = packet as Media.Rtp.RtpPacket;
                
                if (packet == null || packet.Disposed) return;

                if (packet.IsComplete) Console.ForegroundColor = ConsoleColor.Blue;
                else Console.ForegroundColor = ConsoleColor.Red;

                Media.Rtp.RtpClient client = ((Media.Rtp.RtpClient)sender);

                Media.Rtp.RtpClient.TransportContext matched = null;

                if (client != null) matched = client.GetContextForPacket(rtpPacket);

                if (matched == null)
                {
                    Console.WriteLine("****Unknown RtpPacket context: " + Media.RtpTools.RtpSendExtensions.PayloadDescription(rtpPacket) + '-' + rtpPacket.PayloadType + " Length = " + rtpPacket.Length + (rtpPacket.Header.IsCompressed ? string.Empty :  "Ssrc " + rtpPacket.SynchronizationSourceIdentifier.ToString()) + " \nAvailables Contexts:", "*******\n\t***********");
                    if(client != null) foreach (Media.Rtp.RtpClient.TransportContext tc in client.TransportContexts)
                    {
                        Console.WriteLine(string.Format(TestingFormat, "\tDataChannel", tc.DataChannel));
                        Console.WriteLine(string.Format(TestingFormat, "\tControlChannel", tc.ControlChannel));
                        Console.WriteLine(string.Format(TestingFormat, "\tLocalSourceId", tc.SynchronizationSourceIdentifier));
                        Console.WriteLine(string.Format(TestingFormat, "\tRemoteSourceId", tc.RemoteSynchronizationSourceIdentifier));
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
                Media.Rtcp.RtcpPacket rtcpPacket = packet as Media.Rtcp.RtcpPacket;
                
                if (packet == null || packet.Disposed) return;

                if (packet.IsComplete) if(packet.Transferred.HasValue) Console.ForegroundColor = ConsoleColor.Green; else Console.ForegroundColor = ConsoleColor.DarkGreen;
                else Console.ForegroundColor = ConsoleColor.Yellow;

                Media.Rtp.RtpClient client = ((Media.Rtp.RtpClient)sender);

                Media.Rtp.RtpClient.TransportContext matched = null;

                if (client != null) matched = client.GetContextForPacket(rtcpPacket);

                Console.WriteLine(string.Format(format, incomingFlag ? "\tReceieved" : "\tSent", (packet.IsComplete ? "Complete" : "Incomplete"), packetType.Name) + "\tSynchronizationSourceIdentifier=" + rtcpPacket.SynchronizationSourceIdentifier + "\nType=" + rtcpPacket.PayloadType + " Length=" + rtcpPacket.Length + "\n Bytes = " + rtcpPacket.Payload.Count + " BlockCount = " + rtcpPacket.BlockCount + "\n Version = " + rtcpPacket.Version);

                if (matched != null) Console.WriteLine(string.Format(TestingFormat, "Context:", "*******\n\t*********** Local Id: " + matched.SynchronizationSourceIdentifier + " Remote Id:" + matched.RemoteSynchronizationSourceIdentifier + " - Channel = " + matched.ControlChannel));
                else
                {
                    Console.WriteLine(string.Format(TestingFormat, "Unknown RTCP Packet context -> " + rtcpPacket.PayloadType + " \nAvailables Contexts:", "*******\n\t***********"));
                    if (client != null) foreach (Media.Rtp.RtpClient.TransportContext tc in client.TransportContexts)
                    {
                        Console.WriteLine(string.Format(TestingFormat, "\tDataChannel", tc.DataChannel));
                        Console.WriteLine(string.Format(TestingFormat, "\tControlChannel", tc.ControlChannel));
                        Console.WriteLine(string.Format(TestingFormat, "\tLocalSourceId", tc.SynchronizationSourceIdentifier));
                        Console.WriteLine(string.Format(TestingFormat, "\tRemoteSourceId", tc.RemoteSynchronizationSourceIdentifier));
                    }
                }

                if (rtcpPacket.Payload.Count > 0 && writePayload) Console.WriteLine(string.Format(TestingFormat, "Payload", BitConverter.ToString(rtcpPacket.Payload.Array, rtcpPacket.Payload.Offset, rtcpPacket.Payload.Count)));
            }

            Console.ForegroundColor = previousForegroundColor;
            Console.BackgroundColor = previousBackgroundColor;
            
        }

        static void TestRtspClient(string location, System.Net.NetworkCredential cred = null, Media.Rtsp.RtspClient.ClientProtocolType? protocol = null)
        {


            //For display
            int emptyFrames = 0, incompleteFrames = 0, rtspIn = 0, rtspOut = 0, rtspInterleaved = 0, rtspUnknown = 0;

            //For allow the test to run in an automated manner
            bool shouldStop = false;

            if (string.IsNullOrWhiteSpace(location))
            {
                Console.WriteLine("Enter a RTSP URL and press enter (Or enter to quit):");
                location = Console.ReadLine();
            }

            Console.WriteLine("Location = \"" + location + "\" " + (protocol.HasValue ? "Using Rtp Protocol: " + protocol.Value : string.Empty) + "\n Press a key to continue. Press Q to Skip");
            Media.Rtsp.RtspClient client = null;
            if (Console.ReadKey().Key != ConsoleKey.Q && !string.IsNullOrWhiteSpace(location))
            {
                using (System.IO.TextWriter consoleWriter = new System.IO.StreamWriter(Console.OpenStandardOutput()))
                {
                    //Using a new Media.RtspClient
                    using (client = new Media.Rtsp.RtspClient(location, protocol))
                    {                        

                        //Use the credential specified
                        if (cred != null) client.Credential = cred;

                        //Connection event
                        client.OnConnect += (sender, args) =>
                        {

                            if (client.Connected)
                            {
                                //Set a timeout to wait for responses (in milliseconds), they will be recalulcated when the Setup is performed according to information in the SDP.
                                client.SocketWriteTimeout = client.SocketReadTimeout = 0;


                                

                                //Try to start listening
                                try
                                {
                                    consoleWriter.WriteLine("\t*****************\nConnected to :" + client.Location);
                                    client.StartListening();
                                    consoleWriter.WriteLine("\t*****************\nStartedListening to :" + client.Location);
                                }
                                catch (Exception ex) { writeError(ex); shouldStop = true; }
                            }
                        };

                        //Define handles once playing
                        Media.Rtp.RtpClient.RtpPacketHandler rtpPacketReceived = (sender, rtpPacket) =>
                        {
                            TryPrintClientPacket(sender, true, (Media.Common.IPacket)rtpPacket);
                        };

                        Media.Rtp.RtpClient.RtpFrameHandler rtpFrameReceived = (sender, rtpFrame) =>
                        {
                            if (rtpFrame.Disposed) return;
                            if (rtpFrame.IsEmpty)
                            {
                                ++emptyFrames;
                                Console.BackgroundColor = ConsoleColor.Red; consoleWriter.WriteLine("\t*******Got a EMTPTY RTP FRAME*******"); Console.BackgroundColor = ConsoleColor.Black;
                            }
                            else if (rtpFrame.IsMissingPackets)
                            {
                                ++incompleteFrames;
                                Console.BackgroundColor = ConsoleColor.Yellow; consoleWriter.WriteLine("\t*******Got a RTPFrame With Missing Packets PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.Complete + " HighestSequenceNumber = " + rtpFrame.HighestSequenceNumber); Console.BackgroundColor = ConsoleColor.Black;
                            }
                            else
                            {
                                Console.BackgroundColor = ConsoleColor.Blue; consoleWriter.WriteLine("\tGot a RTPFrame("+ rtpFrame.PayloadTypeByte +") PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.Complete + " HighestSequenceNumber = " + rtpFrame.HighestSequenceNumber); Console.BackgroundColor = ConsoleColor.Black;
                            }
                        };

                        Media.Rtp.RtpClient.RtcpPacketHandler rtcpPacketReceived = (sender, rtcpPacket) => TryPrintClientPacket(sender, true, (Media.Common.IPacket)rtcpPacket);

                        Media.Rtp.RtpClient.RtcpPacketHandler rtcpPacketSent = (sender, rtcpPacket) => TryPrintClientPacket(sender, false, (Media.Common.IPacket)rtcpPacket);

                        Media.Rtp.RtpClient.InterleaveHandler rtpInterleave = (sender, data) =>
                        {
                            ++rtspInterleaved;
                            Console.BackgroundColor = ConsoleColor.Cyan;
                            consoleWriter.WriteLine("\tInterleaved=>" + data.Count + " Bytes");
                            //consoleWriter.WriteLine("\tInterleaved=>" + Encoding.ASCII.GetString(data.Array.Skip(data.Offset).Take(data.Count).ToArray()).Replace('\a', 'A'));
                            Console.BackgroundColor = ConsoleColor.Black;
                        };

                        //Handle Request event
                        client.OnRequest += (sender, request) =>
                        {
                            ++rtspOut; Console.WriteLine("Client Requested :" + request.Location + " " + request.Method);
                        };

                        //When the RtpClient disconnects this event is raised.
                        client.OnDisconnect += (sender, args) =>
                        {                            
                            consoleWriter.WriteLine("\t*****************Disconnected from :" + client.Location);                            
                        };

                        //Hanle Media.Rtsp Responses
                        client.OnResponse += (sender, request, response) =>
                        {
                            //Track null and unknown responses
                            if (response != null)
                            {
                                ++rtspIn;
                                if (response.StatusCode == Media.Rtsp.RtspStatusCode.Unknown) ++rtspUnknown;
                            }
                            else
                            {
                                consoleWriter.WriteLine("\t*****************\nClient got response :" + response.StatusCode.ToString() + ", for request: " + request.Location + " " + request.Method);
                            }
                        };

                        //Playing event
                        client.OnPlay += (sender, args) =>
                        {
                            //There is a single intentional duality in the design of the pattern utilized for the RtpClient such that                    
                            //client.Client.MaximumRtcpBandwidthPercentage = 25;

                            ///SHOULD also subsequently limit the maximum amount of CPU the client will be able to use

                            //Add events now that we are playing
                            client.Client.RtpPacketReceieved += rtpPacketReceived;
                            client.Client.RtpFrameChanged += rtpFrameReceived;
                            client.Client.RtcpPacketReceieved += rtcpPacketReceived;
                            client.Client.RtcpPacketSent += rtcpPacketSent;
                            client.Client.InterleavedData += rtpInterleave;

                            client.Client.SetReceiveBufferSize(0x1024);

                            System.IO.File.WriteAllText("current.sdp", client.SessionDescription.ToString());

                            //Indicate if LivePlay
                            if (client.LivePlay)
                            {
                                consoleWriter.WriteLine("\t*****************Playing from Live Source");
                            }

                            //Indicate if StartTime is found
                            if (client.StartTime.HasValue)
                            {
                                consoleWriter.WriteLine("\t*****************Media Start Time:" + client.StartTime);

                            }

                            //Indicate if EndTime is found
                            if (client.EndTime.HasValue)
                            {
                                consoleWriter.WriteLine("\t*****************Media End Time:" + client.EndTime);
                            }

                            foreach (Media.Rtp.RtpClient.TransportContext tc in client.Client.TransportContexts)
                            {
                                consoleWriter.WriteLine("\t*****************Local Id " + tc.SynchronizationSourceIdentifier);
                                consoleWriter.WriteLine("\t*****************Remote Id " + tc.RemoteSynchronizationSourceIdentifier);
                            }

                        };

                        client.OnStop += (sender, args) =>
                        {

                            //Remove events now that we are Disconnected
                            client.Client.RtpPacketReceieved -= rtpPacketReceived;
                            client.Client.RtpFrameChanged -= rtpFrameReceived;
                            client.Client.RtcpPacketReceieved -= rtcpPacketReceived;
                            client.Client.RtcpPacketSent -= rtcpPacketSent;
                            client.Client.InterleavedData -= rtpInterleave;

                            shouldStop = true;

                            consoleWriter.WriteLine("\t*****************Stopping Playback (Press Q To Exit)");

                            if (System.IO.File.Exists("current.sdp")) System.IO.File.Delete("current.sdp");
                        };

                        client.Connect();

                        client.ProtocolSwitchTime = TimeSpan.FromSeconds(10);

                        //Indicate waiting
                        Console.WriteLine("Waiting for connection. Press Q to exit");

                        //Wait for a key press of 'Q' once playing
                        while (!shouldStop)
                        {
                            System.Threading.Thread.Sleep(client.Timeout.Seconds + 1 * 5000);

                            if (client.StartedListening.HasValue)
                            {

                                TimeSpan playingfor = (DateTime.UtcNow - client.StartedListening.Value);

                                if (client.Playing) Console.WriteLine("Client Playing. for :" + playingfor.ToString());

                                if (!client.LivePlay) Console.WriteLine("Remaining Time in media:" + playingfor.Subtract(client.EndTime.Value).ToString());

                                if (client.Connected == false && shouldStop == false) Console.WriteLine("Client Not connected Waiting for (Q)");

                                shouldStop = Console.KeyAvailable ? Console.ReadKey(true).Key == ConsoleKey.Q : false || playingfor > client.EndTime;
                            }
                        }

                        //if the client is connected still
                        if (client.Connected)
                        {
                            //Try to send some requests if quit early before the Teardown.
                            try
                            {

                                Media.Rtsp.RtspMessage one = null, two = null;

                                //Send a few requests just because
                                if (client.SupportedMethods.Contains(Media.Rtsp.RtspMethod.GET_PARAMETER))
                                    one = client.SendGetParameter();
                                else one = client.SendOptions();

                                //Try to send an options request now, if that fails just send a tear down
                                try { two = client.SendOptions(); }
                                catch { two = null; }

                                //All done with the client
                                client.StopListening();

                                if (one == null && two == null) Media.Common.ExceptionExtensions.CreateAndRaiseException(client, "Sending In Play Failed");//Must get a response to at least one of these
                                else Console.WriteLine("Sending Requests In Play Success");

                                if (one != null) consoleWriter.WriteLine(one);
                                if (two != null) consoleWriter.WriteLine(two);


                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                consoleWriter.WriteLine(ex.Message);
                                Console.ForegroundColor = ConsoleColor.Red;
                            }
                        }

                        //Output test info before ending
                        if (client.Client != null)
                        {

                            //Print out some information about our program
                            Console.BackgroundColor = ConsoleColor.Blue;
                            consoleWriter.WriteLine("RTCP".PadRight(Console.BufferWidth - 7, '▓'));
                            consoleWriter.WriteLine("RtcpBytes Sent: " + client.Client.TotalRtcpBytesSent);
                            consoleWriter.WriteLine("Rtcp Packets Sent: " + client.Client.TotalRtcpPacketsSent);
                            consoleWriter.WriteLine("RtcpBytes Recieved: " + client.Client.TotalRtcpBytesReceieved);
                            consoleWriter.WriteLine("Rtcp Packets Recieved: " + client.Client.TotalRtcpPacketsReceieved);
                            Console.BackgroundColor = ConsoleColor.Magenta;
                            consoleWriter.WriteLine("RTP".PadRight(Console.BufferWidth - 7, '▓'));
                            consoleWriter.WriteLine("Rtp Packets Recieved: " + client.Client.TotalRtpPacketsReceieved);
                            consoleWriter.WriteLine("Frames with missing packets: " + incompleteFrames);
                            consoleWriter.WriteLine("Empty Frames: " + emptyFrames);
                            Console.BackgroundColor = ConsoleColor.Cyan;
                            consoleWriter.WriteLine("RTSP".PadRight(Console.BufferWidth - 7, '▓'));
                            consoleWriter.WriteLine("Rtsp Requets Sent: " + rtspIn);
                            consoleWriter.WriteLine("Rtsp Responses Receieved: " + rtspOut);
                            consoleWriter.WriteLine("Rtsp Missing : " + (client.ClientSequenceNumber - rtspIn));
                            consoleWriter.WriteLine("Rtsp Interleaved: " + rtspInterleaved);
                            consoleWriter.WriteLine("Rtsp Unknown: " + rtspUnknown);
                            Console.BackgroundColor = ConsoleColor.Black;
                        }

                    }
                }
            }
        }

        static void TestSdp()
        {
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

            sd = new Media.Sdp.SessionDescription(@"v=0
o=- 1183588701 6 IN IP4 10.3.1.221
s=Elecard NWRenderer
i=Elecard streaming
u=http://www.elecard.com
e=tsup@elecard.net.ru
c=IN IP4 239.255.0.1/64
b=CT:0
a=ISMA-compliance:2,2.0,2
a=mpeg4-iod: ""data:application/mpeg4-iod;base64,AoE8AA8BHgEBAQOBDAABQG5kYXRhOmFwcGxpY2F0aW9uL21wZWc0LW9kLWF1O2Jhc2U2NCxBVGdCR3dVZkF4Y0F5U1FBWlFRTklCRUFGM0FBQVBvQUFBRERVQVlCQkE9PQEbAp8DFQBlBQQNQBUAB9AAAD6AAAA+gAYBAwQNAQUAAMgAAAAAAAAAAAYJAQAAAAAAAAAAA2EAAkA+ZGF0YTphcHBsaWNhdGlvbi9tcGVnNC1iaWZzLWF1O2Jhc2U2NCx3QkFTZ1RBcUJYSmhCSWhRUlFVL0FBPT0EEgINAAAUAAAAAAAAAAAFAwAAQAYJAQAAAAAAAAAA""
m=video 10202 RTP/AVP 98
a=rtpmap:98 H264/90000
a=control:trackID=1
a=fmtp:98 packetization-mode=1; profile-level-id=4D001E; sprop-parameter-sets=Z00AHp5SAWh7IA==,aOuPIAAA
a=mpeg4-esid:201
m=audio 10302 RTP/AVP 96
a=rtpmap:96 mpeg4-generic/48000/2
a=control:trackID=2
a=fmtp:96 streamtype=5; profile-level-id=255; mode=AAC-hbr; config=11900000000000000000; objectType=64; sizeLength=13; indexLength=3; indexDeltaLength=3
a=mpeg4-esid:101");

            Console.WriteLine(sd.ToString());

            //Get a few attributes

            Media.Sdp.SessionDescriptionLine mpeg4IodLine = sd.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("mpeg4-iod"))).FirstOrDefault();

            Media.Sdp.SessionDescriptionLine connectionLine = sd.ConnectionLine;

            //make a new Sdp using the media descriptions from the old but a new name

            sd = new Media.Sdp.SessionDescription(0)
            {
                //OriginatorAndSessionIdentifier = sd.OriginatorAndSessionIdentifier,
                SessionName = sd.SessionName,
                MediaDescriptions = sd.MediaDescriptions,
                TimeDescriptions = sd.TimeDescriptions,

            };                       

            //Add a few lines from the old one

            sd.Add(connectionLine);

            sd.Add(mpeg4IodLine);

            Console.WriteLine(sd.ToString());

            Console.WriteLine(mpeg4IodLine.ToString());

            Console.WriteLine(connectionLine.ToString());

            sd = new Media.Sdp.SessionDescription("v=0\r\no=StreamingServer 3219006789 1223277283000 IN IP4 10.8.127.4\r\ns=/sample_100kbit.mp4\r\nu=http:///\r\ne=admin@\r\nc=IN IP4 0.0.0.0\r\nb=AS:96\r\nt=0 0\r\na=control:*\r\na=mpeg4-iod:\"data:application/mpeg4-iod;base64,AoJrAE///w/z/wOBdgABQNhkYXRhOmF\"");

            Console.WriteLine(sd);

        }

        /// <summary>
        /// Tests the Media.RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        static void TestServer()
        {
            //Setup a Media.RtspServer on port 554
            Media.Rtsp.RtspServer server = new Media.Rtsp.RtspServer()
            {
                Logger = new Media.Rtsp.Server.RtspServerDebuggingLogger()
            };
            
            //Should be working also, allows rtsp requests to be handled over UDP port 555 by default
            //server.EnableUdp();

            //The server will take in Media.RtspSourceStreams and make them available locally

            Media.Rtsp.Server.Streams.RtspSourceStream source = new Media.Rtsp.Server.Streams.RtspSourceStream("Alpha", "rtsp://quicktime.uvm.edu:1554/waw/wdi05hs2b.mov")
            {
                //Will force VLC et al to connect over TCP
                //                m_ForceTCP = true
            };

            //server.AddCredential(source, new System.Net.NetworkCredential("test", "test"), "Basic");

            //If the stream had a username and password
            //source.Client.Credential = new System.Net.NetworkCredential("user", "password");
            
            //Add the stream to the server
            server.AddStream(source);

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("AlphaTcp", "rtsp://quicktime.uvm.edu:1554/waw/wdi05hs2b.mov", Media.Rtsp.RtspClient.ClientProtocolType.Tcp)
            {
                //m_ForceTCP = true
            });

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Beta", "rtsp://inet.orban.com:554/tropic.3gp")
            {
                //m_ForceTCP = true
            });

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("BetaTcp", "rtsp://inet.orban.com:554/tropic.3gp", Media.Rtsp.RtspClient.ClientProtocolType.Tcp)
            {
                //m_ForceTCP = true
            });        

            //H263 Stream Tcp Exposed @ rtsp://localhost/live/Alpha through Udp and Tcp (Source is YouTube hosted video which explains how you can get a Media.Rtsp Uri to any YouTube video)

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Gamma", "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp"));

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("YouTube", "rtsp://v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/video.3gp"));

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Delta", "rtsp://46.249.213.93/broadcast/gamerushtv-tablet.3gp"));

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Omega", "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"));

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Turbo", "rtsp://211.79.36.213/discoveryturbo_gphone.sdp"));
            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("TurboTcp", "rtsp://211.79.36.213/discoveryturbo_gphone.sdp", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));

            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("Science", "rtsp://211.79.36.213/discoveryscience_gphone.sdp"));
            server.AddStream(new Media.Rtsp.Server.Streams.RtspSourceStream("ScienceTcp", "rtsp://211.79.36.213/discoveryscience_gphone.sdp", Media.Rtsp.RtspClient.ClientProtocolType.Tcp));

            //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/PicsTcp through Tcp
            server.AddStream(new Media.Rtsp.Server.Streams.RFC2435Stream("PicsTcp", System.Reflection.Assembly.GetExecutingAssembly().Location) { Loop = true, ForceTCP = true });

            Media.Rtsp.Server.Streams.RFC2435Stream imageStream;// new Media.Rtsp.Server.Streams.RFC2435Stream("SamplePictures", @"C:\Users\Public\Pictures\Sample Pictures\") { Loop = true };

            //Expose Bandit's Pictures through Udp and Tcp
            server.AddStream(imageStream = new Media.Rtsp.Server.Streams.RFC2435Stream("Bandit", string.Join(string.Empty, new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location).Segments.Reverse().Skip(1).Reverse().ToArray()) + "\\Bandit\\") { Loop = true });

            //Test H.264 Encoding
            server.AddStream(new Media.Rtsp.Server.Streams.RFC6184Stream(240, 160, "h264", System.Reflection.Assembly.GetExecutingAssembly().Location) { Loop = true });

            //Test MPEG2 Endoing
            server.AddStream(new Media.Rtsp.Server.Streams.RFC2250Stream(128, 96, "mpeg2", System.Reflection.Assembly.GetExecutingAssembly().Location) { Loop = true });

            //Test Http Jpeg Transcoding
            server.AddStream(new Media.Rtsp.Server.Streams.JPEGSourceStream("HttpTestJpeg", new Uri("http://118.70.125.33:8000/cgi-bin/camera")));

            server.AddStream(new Media.Rtsp.Server.Streams.MJPEGSourceStream("HttpTestMJpeg", new Uri("http://extcam-16.se.axis.com/axis-cgi/mjpg/video.cgi?")));

            //TODO
            //server.RequestReceived event
            //server.ClientConnected / ClientDisconnected

            Media.Rtsp.Server.Streams.RFC2435Stream screenShots = new Media.Rtsp.Server.Streams.RFC2435Stream("Screen", null, false);

            server.AddStream(screenShots);

            System.Threading.Thread taker = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart((o) =>
            {
                using (var bmpScreenshot = new System.Drawing.Bitmap(Screen.PrimaryScreen.Bounds.Width,
                           Screen.PrimaryScreen.Bounds.Height,
                           System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {

                    // Create a graphics object from the bitmap.
                    using (var gfxScreenshot = System.Drawing.Graphics.FromImage(bmpScreenshot))
                    {

                        //Forever
                        while (server.Listening)
                        {
                            // Take the screenshot from the upper left corner to the right bottom corner.
                            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                                        Screen.PrimaryScreen.Bounds.Y,
                                                        0,
                                                        0,
                                                        Screen.PrimaryScreen.Bounds.Size,
                                                        System.Drawing.CopyPixelOperation.SourceCopy);
                            
                            //Convert to JPEG and put in packets
                            screenShots.Packetize(bmpScreenshot, 75);

                            //REST
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
            }))
            {
                Priority = System.Threading.ThreadPriority.Highest
            };

            //Start the server
            server.Start();

            taker.Start();

            //If you add more streams they will be started once the server is started

            Console.WriteLine("Listening on: " + server.LocalEndPoint);

            Console.WriteLine("Waiting for input...");
            Console.WriteLine("Press 'U' to Enable Udp on Media.RtspServer");
            Console.WriteLine("Press 'H' to Enable Http on Media.RtspServer");
            Console.WriteLine("Press 'T' to Perform Load SubTest on Media.RtspServer");
            Console.WriteLine("Press 'F' to See statistics for " + imageStream.Name);

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Q) break;
                else if (keyInfo.Key == ConsoleKey.H)
                {
                    Console.WriteLine("Enabling Http");
                    server.EnableHttp();
                }
                else if (keyInfo.Key == ConsoleKey.U)
                {
                    Console.WriteLine("Enabling Udp");
                    server.EnableUdp();
                }
                else if (keyInfo.Key == ConsoleKey.F)
                {
                    Console.WriteLine("======= RFC2435 Stream Information =======");
                    Console.WriteLine("Uptime (Seconds) :" + imageStream.Uptime.TotalSeconds);
                    Console.WriteLine("Frames Per Second :" + imageStream.FramesPerSecond);
                    Console.WriteLine("==============");
                }
                else if (keyInfo.Key == ConsoleKey.T)
                {
                    Console.WriteLine("Performing Load Test");
                    SubTestLoad(server);
                }
                else if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }

                System.Threading.Thread.Yield();
            }

            server.DisableHttp();

            server.DisableUdp();

            Console.WriteLine("Stream Recieved : " + server.TotalStreamBytesRecieved);

            Console.WriteLine("Stream Sent : " + server.TotalStreamBytesSent);

            Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

            Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

            Console.WriteLine("Stopping Server");            

            server.Stop();
        }
        
        /// <summary>
        /// Tests the Rtp and Media.RtspClient in various modes (Against the server)
        /// </summary>
        static void SubTestLoad(Media.Rtsp.RtspServer server)
        {
            //100 times about a GB in total

            //Get the Degrees Of Parallelism
            int dop = 0;

            if (server.HttpEnabled) dop += 2;
            if (server.UdpEnabled) dop += 2;
            dop += 3;//Tcp

            //Test the server
            ParallelEnumerable.Range(1, 100).AsParallel().WithDegreeOfParallelism(dop).ForAll(i =>
            {
                //Create a client
                if (server.HttpEnabled && i % 2 == 0) 
                {
                    //Use Media.Rtsp / Http
                    using (Media.Rtsp.RtspClient httpClient = new Media.Rtsp.RtspClient("http://127.0.0.1/live/Alpha"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Http / Media.Rtsp Test");

                            httpClient.StartListening();

                            while (httpClient.Client.TotalRtpBytesReceieved <= 1024) { }

                            Console.WriteLine("Test passed");

                            return;
                        }
                        catch(Exception ex)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rtp / Http Test Failed: " + ex.Message);
                            Console.BackgroundColor = ConsoleColor.Black;
                            return;
                        }
                    }
                }
                else if (server.UdpEnabled && i % 3 == 0) 
                {
                    //Use Media.Rtsp / Udp
                    using (Media.Rtsp.RtspClient udpClient = new Media.Rtsp.RtspClient("rtspu://127.0.0.1/live/Alpha"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Udp / Media.Rtsp Test");

                            udpClient.StartListening();

                            while (udpClient.Client.TotalRtpBytesReceieved <= 1024) { }

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
                    //Use Media.Rtsp / Tcp
                    using (Media.Rtsp.RtspClient tcpClient = new Media.Rtsp.RtspClient("rtsp://127.0.0.1/live/Omega", i % 2 == 0 ? Media.Rtsp.RtspClient.ClientProtocolType.Tcp : Media.Rtsp.RtspClient.ClientProtocolType.Udp))
                    {
                        try
                        {
                            Console.WriteLine("Performing Media.Rtsp Test");

                            tcpClient.StartListening();

                            tcpClient.ProtocolSwitchTime = TimeSpan.FromSeconds(1);

                            while (tcpClient.Client.TotalRtpBytesReceieved <= 4096 && tcpClient.Client.Uptime.TotalSeconds < 10) { System.Threading.Thread.Sleep(100); }

                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.WriteLine("Test passed " + tcpClient.Client.TotalRtpBytesReceieved + " " + tcpClient.RtpProtocol);
                            Console.BackgroundColor = ConsoleColor.Black;

                            tcpClient.StopListening();

                            return;
                        }
                        catch(Exception ex)
                        {                            
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rtp / Tcp Test Failed: " + ex.Message);
                            Console.BackgroundColor = ConsoleColor.Black;
                            return;
                        }
                    }
                }                
            });
        }

        static void TestRtpFrame()
        {
            //Create a frame
            Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0);

            //Add packets to the frame
            for (int i = 0; i < 15; ++i)
            {

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Utility.Empty)
                {
                    SequenceNumber = i,
                    Marker = i == 14
                });
            }

            if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

            if (!frame.Complete) throw new Exception("Frame is not complete");

            if (!frame.HasMarker) throw new Exception("Frame does not have marker");

            //Remove the marker packet
            frame.Remove(14);

            if (frame.Complete) throw new Exception("Frame is complete");

            if (frame.HasMarker) throw new Exception("Frame has marker");

            if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

            //Remove the first packet
            frame.Remove(1);

            if (!frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
        }

        static void TestJpegFrame()
        {
            Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame f;

            //Create a RFC2435 Jpeg from a (RFC2035) Jpeg, Any `valid` JPEG should do.
            using (var jpegStream = new System.IO.FileStream("video.jpg", System.IO.FileMode.Open))
            {
                //Create a JpegFrame from the stream knowing the quality the image was encoded at (No Encoding performed, only Packetization)
                f = new Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame(jpegStream, 25);

                //Save the JpegFrame as a Image (Decoding performed)
                using (System.Drawing.Image jpeg = f)
                {
                    jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)
                System.IO.File.Delete("result.jpg");
            }
            
            //Create a JpegFrame from existing RtpPackets
            using (Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame x = new Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame())
            {
                foreach (Media.Rtp.RtpPacket p in f) x.Add(p);

                //Save JpegFrame as Image
                using (System.Drawing.Image jpeg = x)
                {
                    jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }                                

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            //Create a RFC2435Frame from an existing image and store the quantization tables in the frame.
            using (var image = System.Drawing.Image.FromFile("flip.jpg"))
            {
                Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame t = Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame.Packetize(image, 100);

                using (System.Drawing.Image jpeg = t)
                {
                    jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                System.IO.File.Delete("result.jpg");
            }

            byte[][] jpegPackets = new byte[][]
                {
                    Utility.HexStringToBytes("801ac9e26c6b82a24dce0864000000004132381f0054ffffe5d6a414c5a916980e029d4829d400628c514b400507a514374a40463bd4a053179c7d6a414864b10e33daa602a28ba63f1a9450264d6eb9901f4a9a6fba05456df7cfd2a4b8246d18a04478e0d14849c1e307de8f98fa0fd6980ea514d19c727f2e29c00c7393f8d0038714fcfcabf53fd2a30a318ebf539a7055ce42afe5400ec80704807eb4a0823820fd29013da9f92437278534804073d9bf234bf81a3340a6028cf391d7dc52e4e3803f3a052e69000dddc2fe7ffd6a5e72064027f1ff003d68a33cafe3fd28017e6c755ffbe7ff00af4a037f787e5ffd7a334b400a79183c7d29029ebb8fe94a3a75a5eb40114884ff0019fd2a0492f6d8ba40c8413bbe64cfb7f4ab6464f14dda777e1fd4d00402f7551c6e84ff00db214efb76ad9e1e1e9de21530534bb4d0043f6fd5874687fefd0a8eeaff005492dda291e2d8e30711804fe3568a9c5412c6cd4f4032824bceec7e140593241038f7ad1f20fa5279077671d40a7a08cf226eca3f127fc29009fba2fe67fc2b47c86f4a3c83e940144799dc0fc29a5dc0cf96c7f2ad0f24fa527927d280288924ce3ca61f88a77da9a32003267fd9563fcaae793c8e29a22c81914015a3d69d64d897b2ab7a6f22ac9d62f3690d792952390cf907f3a0c4076a4f287a5164045f6b59410c903e7ae23507f31cd355adf3f35b447fe04c3fad4e60523a537ecd1ff7450529497521f2ad59b263907b2c800fd41ab766d696cce544c778031907f962a1fb32f3d7f334f8ad3272ae411ef43437524d59b3654ee5070467b375ac6d7acf7a19d3a8fbdfe356196ef18172c47e14d93ed6c9b1ca3afbafff005e85a1072b22ed07a5462674380d91ee2aeea16d240edb9786e47bd669041071d2958b4ec5e5a7ad3074a914532478a5a414b40052d14521852374a5a6b9e0d0009ce2a414c5eb4f1523278f951ea38a90531704023d29e2989966dbf8a966fbe3e945b8c2b1f7c536627cde3047d6810946690838ce47e549827ab1fc2801c7a7f9f5a75340ec4934a1476247e26801dd29770032481f8d376af7507f0a55c0381c71db8a0072ba1fe35fce9ea701860f2a7a0a6034ea0050dc7009fc28dc47f09fd3fc6814500286c8fba47d697e6c8c053f8fff005a85383f851400a198e0607e7ffd6a71dc71c8e33dbe94da5a003e7fef2ffdf27fc69c377f787e5ffd7a4140e2900edd8ea7f4a7727b9a631f947d453b3400b8ff0069bf4a503dcd3734b9a00500e3ef1fd29467fbc69b9a33c50029381924fe9487af7a6b1f90fe1fcc519cd301dc01de8279c8eb4dcd2d001923d0d197edb7f2a3345001cf723fef9ffebd2faff85349e07d7fa1a5a003273f747e74a07a85a334669dc0085f4cfe149853fc3fca9734669008557b8a4d8a4f43f952e73f9d2f14ee030c480f5c53d220071400339a7601ebd6810bb290ad2ed19e9f95371900f3f9d0052d52d4cf6c71f7979fad72b30dbb97bd76e41f5ac2d4f4cc192753f2804914d6a34670e9520a60a916801c28a296800a28a5a004a6b73c53e98c32690c72f534f14d4e87eb4f03240a4326518e076269e29a3900fad3c5325966dc1f2c9f7a6487329a9213fbb5a89c1f30e0f7f4a5d400d148727be31e9401fed1fd2980ea75315413d4fe74b8fae3dcd201d4ece08edc7f8d302803ee8fca9c303b0fca9806f51fc429db8638607e9464f1c9a72f240340081b3d8fe4680c3d0fe469173b17af4a7520004b03856e7a76a70247546fd3fc693f0a5a00371e9b1bf4a033ff0073f5a514a38073400996271b571fef7ff5a9d96e9b57fefaff00eb52528a0018bed230bf9fff005a90193d1697a52f7a004fde7aa0fce973274cafeb477a70e87fcf7a40372fdcafe5465b1d4528eb45301a7710471f97ff005e8cb67ffad4ec51400dcb7a0a5cbff747e74ea3191400d05b1d07e74b96f6fcff00fad4eeb49400d25b03a139ed9ff0a379cfdd34ec514009b89e369fcc51bbdbf514b46060"),
                    Utility.HexStringToBytes("801ac9e36c6b82a24dce0864000005904132381f0054ffff50026f19e87f2a5cfb1fc8d181463da8010301c1207e346f5fef0fce9481e951cb3470ff00ac9029f4cf3f95004a187ad3b354fedb69de61ff007c9ff0a9167b76008962fc58034c45acd266abbc91201c924f6534c92e4229f2c166f73c5202cb100124e07a9acbd4b50b77b3963490ef3c0e383f8d57ba9ae66c873f2fa0e05526423a8229ad067fffd0e6c548b4c1520a621d4514b4c0051452d2012987ef0a90d33196a431c83826a4438653ef4d5fbb4e033fce901328c000f514f1483ad380aa116a21845a84f2e6ac27dc5c7a556e72791f95480a69074a0a9f523e94801fef1a603a9d4dda3dff003a5031d09fccd003852d2607700d1b5777dd5fca801c08f5a55650c0b30033eb48147a0fca9e09c75a0062b2850370e9eb4f0ca7f887e7467d697f1a003231d68c8f7fca9c3a3727ee9fe547e3400d0c3d0ffdf269d95c1eb9f4da6968a402647be7e86947e3f952e3de96801b9e78068ddec7f1c53c7193ec7f95371c50026f3fdd3f98ff001a50f9cfc87f31fe34b8a5a00404f5d87f31fe3464ff0071bf4ff1a5a3bd0210b3019f2cfe63fc69771e30a48c7b7f8d2ae7701ef4d8ff00d5afd2818b96ec8c7f2ff1a3271f71bf4a7514084ddfec9fca82ca3d7f234b9a5ebde801bb97b13ff7c9ff000a370ff6bfef934e1d40f7a6a93b475e94c00b0079047d41a37a91d452d19c77a40207539e40e7b9146f51fc4bf9d2e4d35dd510b390147249a00cfd5351f20f93037ce47ccc3f87ff00af58de61ce49249a8679ccb3bb9eacc4d393e6207a9c55dac3274cbb6d5193e956e3b60a3748727fba0d321dd0ae026d1ea475ab1b95d772f38ea3d286c405fd3a5391b70eb51a8c92a0f5e94c864d9361ba1a9026f95ced270e2a3911979ebef4eb888839fc8d352e0f46eb4c0a02a45a81240c323f1a90381401352679a8bcca50fcd171930a5a6a9a750021a60fbff853e9a07cf9a0648a3814f41938f634d5e83e94e4e1c7d6a409c528eb40a515422daf083e955c77ab2d800fb0aad8273cd20148e47d2905260ff78f1f4a500ff78fe9400e14a29b838ce7da9c063de80140a5fe2fc29001e94bb47a7ea6980a29c3d69a140e31fa9a70008c60700e334805a00a4dab8fb8bf952854fee2fe54c0763e56ff0074e3f2a5c7b534469c7eed78f6a5d887f817f2a005e9d6972319cf140007455fc8538e319c0e0e3a5201a31cf238a3207714bf97e54b9fa7e54009b970791c8346e1ea297d38fd2941fa7e5400dde83ab0fce93cc41fc43f3a7d2124e39ffebd0037cc4e9b87e746f43c6f5fce9f934162473400d8dd19c00c0f3d8d355d0281b8640ee7a54993fe4527e03f2a004f313fbebf9d2865ecc3f3a383d71f95040c1f9474f4a042e41c7239e9ef4b9c5376a631b17f2a5d898e113f2a431473401814df2a31c845fca8d898e514fe14c43a8a6f969fdd1f951b107f08a005c5676bae534d6c1c0660a7dfbff4ad0daa3b7ea6b3f5c883e9923027e421b193eb8feb42dc0e633cd6869d1237ef646c60e17dcd45a7d9fda5fcc7e224ebc753e95a6ca493b0231ff6b93f8537d860c7e5e0e47e62a03218dc3af1ea29cb3491365a31b3a103a53e4891d77c6721bb51610b90db644c61b8fa1a64ea7938c11cd560ed03151f74f6ab41c4a80f714016addc5c5be09f987bd56910027b54769298ae31d89c62afdc440fcca3834d7603934b96120feee79fa55f06b24301d56b4a0cf960b1249ee6a6dd06a362714a29b4a290c951aa4cd400e29e1a9dc093b5087934d07342fde3f4a00997ee8a72fde07d0d347000a7c7d4ff009ef40c9c538534528a64975feeb7d2ab0a9d8fca73e9558671d6900ea052156f5ebfe7d2902b7f7cfe9fe1401263e51f5ff1a514cc1c6371fd29429fef9fd2801e053853304f563f90a7153c7cc7a7a5301c29c3a1fa530039fbc6976f1f78f3400e5e94b4c0a3fbcdf9d3828f56fce801d4b4985c1c96e013d68c0ff6bf3a403a97"),
                    Utility.HexStringToBytes("801ac9e46c6b82a24dce086400000b204132381f0054fffff83fe043f91a610b8eff009d280a3fbd8ff7a801d9a334d2abfed7fdf546d4ff006bf3a007514c2ab83f7b804fdea36af4cb7fdf5400ff006a0e083cd370bfed7fdf546140c7cdcf5f9a801d4714ddaa07f1ff00df549b54ff007ffefaa007d19a6054f57ffbea8c023abfe74807d04fcadf4a60007f13fe74b8c64091b07e94c07f18a01a660e7efb0fc07f851b7fe9a37e43fc29012525371e8edfa7f851b4e4e5db8fa7f85310fa4ec69a41cfde3fa7f8520c83f78d201c4d473a09609232400ca5727a0c8eb4ac4ff7bf4a8249776554823d453b814ed63482d520ce081cfb9ef4e28072a391cf14d650463a9a760140dc8f7140036d201c707f4a80a142c8bd3ef2fb1a9c125581fd2a22dcfb8aa02b4c04aa4e30c2a04768ce71c54ec76b93400b93e9d7148632470cc1d7f1ad5b7904d6c0e791c1acd923001c0c77a9f4f930ac87a75a13d44cffd1e018735aabd2b2dba8ad45e940d8f14b4828a403e9453452d00480d2a7de63ed4ccd393bd3113835245cb5462a48bef50b702714e5eb4c14f4fbc3eb4c92d9fbadf4aae3a5586fbadf4aaa338edf95201cddbe9452127da8e7da818e14e14d00fa81f85033ea3f2a0078a7139c7b0c5339f51f952807aeefaf14c43c52f14c00fa8fca972402723039e9400fa3eb4d00fafe94b83fde1f95031dd9bfdd3fca97a5306ef518208e9465b1fc39a403e8cd34163d428fc68f9bb053f8d003e9293e6edb7f5a4c9c76fce801dd430ff64ff2a3229a0b772a7f1347cc7a6dfce801d9a334d39cff000fe67fc28c1f55fccff85003b34669b83eabefc9ff000a5c1f55c7d4ff008500283c8a4e9c7bd261bfd9c7d4ff0085261b24928727fbc7fc2801d9a5cf34986ff67fefa3fe1498600e42f1ee7fc2900ecd28351e5b3fc3ff007d1ff0a0eecff0ff00df47fc2981266827e63f5f5a8f2c38c2fe7ffd6a425813f77f3a404b9a696a8f79f41f9d4534db46de869004f30cec04e3be2a14932c78c7a544ec7b8eb429c8aa403f8009c71dfda9c32808c6e5ef4d00920746ec734e39ce7eeb77aa4891a7e53b94f06a29796c8e2a46ef8fc4544fd29b1a2bc9d314c57c01ec69d272bef50ab135032c96ca1ef4c47d8fc1a01dd190bf7b1d2aab9753c8a10194dd6b517a565b75ad4031414f71d4a2929452014528a414b400ea7af7a60a7af7a6226ef5245f7aa3a922fbd4202714f8ff00d62fd453053e3e245fa8aa24b6c3e571ed55874ab2ff0075be955097ed8c7d2900ea29997cf6fc07ff005e9416effcbffaf40127f0e7de814c0cd8c1e99a505bd07e74012528a8f737f747e74ecb7a0fcfff00ad400f0697aab7fba7f953327d07e746e3b4fca7a1140126697351873fdc6fd29777fb27f4a007e68cd3777b1a4dc7fbadfa5031f9a527f9d30b74055bf2a378c1f95bafa5003f3453030feebfe546f07b1ffbe4d003a8a6eef66ffbe4d2e7d9bfef93400b9a3b1e33d3f9d349f66ffbe4d058608c1e7fd934807514ddc3d0fe546e1eb400e3499a6ef5f5a4f317fbc2810fcd19e0fb8351f98bfde14d32af76a00973485aa1332e3ef0fce9a6653d0d219317a6990d426418f5a8ddd88eb8149b1a43e5b9daa554fcdd33e95485cb070b2e39fe2ff1a7b1a608f77515372f97427dd81c83b73c8ee28385c11f75b91ef512830e327e4f53dbff00ad522edc9560769ea07f31ef5ac75337a0866f2d4b7240a48f5156f9641c763515cc676ed5391fceb3d8943822af624da322939073ed51b904706b285e14e0838a78be51f36e1523b16e4e149aa72c9b109ce297fb421c105bf0c550b99c4bc20217de95865986e9b821b9ab22549461bad62d488ee07cac7e9400d6ed5a8bcf3596ddab4a1ff56bf414ba14f724a5ef45148428a5a414b40c70a70ea7e94d1da9c3a9fa53113d4917dea8874152467069f502714f4fbcbf5a60a72fde1cd324bac72aff004aaa3a54cc7e56fa55604fd45201c4fa500d34b1c7ddcfe34819b3"),
                    Utility.HexStringToBytes("801ac9e56c6b82a24dce0864000010b04132381f0054fffff77f5a00929d5182719c714e073d8d031d4bdcd373ed4e279ffeb502141a5a6823dff2a51d3a11f85301d45276ea3f3a3728ea40a431d9c0a5a6ee5c1f987d3346e5c7de1f9d003e8e948083d0e47ad008f5a042d1476a3f1a06049d8719cf1fce8cfbd1fc2df87f31494085dc71d4d1b8fad373de93340c76e3eb49be9a4d34d201c5cf3834cde71d4d2134d270326900e2e7b1a6b4840e4d44d30e8bc9a66d673f3526ca516c93ce24e14fe34e5567fbc49a548c0a99462a7565a8a433ca02a274c022a777da0e0556676249c50323f28934e58c8a91589ed4166a120b8c75f9483d0f159d1cdf6693c9909f2fa29f4f6abd2be0649a856249183c8623e818918fc856a8ce43649719eebe955e4d8e7fbdfceb44db41275b8419ff006bfc6952c628db2971183ea581ad2cccae604ef1a292b8354ddcb9c9fcab6b58b2fdcf9e2589994fcc170091d33c562e322a5ab14840334edb428a91690c848a54ce78a95d011c546a763f3d2803ffd2e04fdd15a507faa4ff00747f2ace6185fc6b42d8ee850e7b629805c4fe4a0200249c60d5717ef9e5171ed9a2fcf2833eb552a40bc35019ff005440ff007b3527dbe1cf01ff00103fc6b368a606bfdb2df1feb47e47fc2a78dd24c9460dc763586ab9fe203eb5a3a6295f34641e9c839f5a76d00d25fba3e94f4ebfe7d6a35e829ebf7852ea05814e069a296a892d1380df4a80743531ef55d4919e32290c75029b939e94a1b9fba6801ffc3f8d2d341f969c0e7b1a005a5a4e9d69411f4fad021714e1de99b973d47e74ecf0453187a5281834528a0051fa51f8500d1f4a004dabe83f2a36af1f2af3df14bd694f6fc7fa521081547f0afe548550ff0008a75140c618d08c6da6ac11a8c2a607a0a9290f1400d31a63a7eb48513fbbfa9a7122a369157bd201485f43f99a6909e873f5a699189e17f3a6ec24658fe149b4528b06741c0049fa9a80ab31e4f156360c7149b38a9bb2d4522248c0a9940f4a43c0a4078a56289460d2e714c069bbc034c43d8e2984f34d3275a89e6029d809d719a64a76f3daaac972171cf354afaf5f688d4904f24d52422d2cc5e4dc4371d306ac09037024247a15008fe79ac8b7bac48be601e9bb15a22fe204e0003b605691b33195c9c24cc542ca013d8afb5485275e1a407fe00b555efa238c4ae08fee81c5385fdbb280f2c84f73cd55913a84aaecacace30c307e51d2b9d9a330ccd193d0f5f5add92e6d9ba3c86b335058982bc4493d1b2293486afd8a629ea6983919a70a82897b544eb5229a185301b263cafd6acda902dd3d79cfe755ca3328152421a35c75a8191de106618ecbcd57a9e546672d8eb4d68994904107de9a8b15c8a9f1ecddf38247b1c50c9800e73ebed4f8770395e9f81f7fe95496ba85c7791bbee06c9e83afa7f88ab7a6a85f34039cedea31dbff00af54c8f9b9e08c01807d3ffad57ac881348a083919c83ee47f9f6c5392424cbebd053d3ef0a8d4f14f53cd665966969a2973544167a8f7a857bd48a781eb8a8416c9f978a9ea31d4b4c2cdfddcd01cff0074d302414e15187cf634e0c33dff002a0078fad2ff001114cde3b9fce94b0dfd410475cd003e82148c1507ea29052170a2801f85c63031f4a8257f98220efc9a492e02b0fee914d170992697a1a463d5976309b39a46319e15ab366be646564380a0e7dfa5576bd6770ea31ea2914ec6fa44ac9f78e69ad132ff00137e9542def43460670c0d69c13aca8324669ea269321c7fb47f4a4fab1fcaa6c2b923a1151c96fcf2c78a5727908d9c01f7b9fa530b3b1f947e952a46818014e718ed4aeca505d4ac6373cb1fca85451daa53934dc019e691495b6131934bb714d2d8a6efc7534ec03ce05349c75a8da403a530c993cd1615c91a8c8c542d2530cdd853b0ae4acc73d6a17723bd2fcc7a71f5a679633963ba98ae337b374e698e0f526a72706a1734137206cfa552bc189578c7cbfd6af1aa57873301e8b40884"),
                    Utility.HexStringToBytes("801ac9e66c6b82a24dce0864000016404132381f0054ffff54ab92a00f4a881a9e219142286ed39ebcd1b5bd6a62bcf4ed46da762799918527bd0d16e523352e29d8a2c2bb3331b588a51525d2ed9c9ec79a8fbd302453cd39ba546a6a4ea2802c04f6a369a9714d2547522a0a23d84f6a3611c8e3e94e32a8ef4c338a3501ad0ee1b7a0a58a31164e18fd1b149f68fca9a66f73549b42b21cca36eddc71fed0e9d7fc6a7b5e26e0e783fd31fa0aabe771de93cdc76aae662b1b0b923a5381c1e6b14cb4799e82a0a3a0de80f2cbf9d1e7443aca83eac2b037b7f74d26e3e94c563a51756eaa333c7d3b3034c49e220b09171ea4e2b9e0fc82714a650c72319c629580e83ed5075f393f3a05d5bffcf64ffbeab9edc7dbf3a4dcde869858e8fed70631e727fdf54f173013feba3ffbe85731b9b1d0d1bcfbd0163a913427fe5ac7ff007d0a63dc463a3a9ff810ae677fd68f30fbd034748668f190c9cfa11514b70369c3027eb581e67b9a3cc38ea6958ae6b6c8d9331231511663d2b2f79f5346f3ea6958398d0604f079a31c702b3f71f5a37fbd3b05cd152c3b60d5db6bb652339ac2de7fbc68dedfde340ae75f0dd2b364b01f8d5cf36271f7c67eb5c2ef6c1f98fe74798d8fbe7f3a7a05cec4baab7de1f9d23cc3b11f9d71fe637f7cfe749e637f7cfe74ac87cc755e70276f269a5ce7815cbf9aff00df3f9d1e6bff007cfe740731d2b39eb511973c0ae7fcd7fef9fce8f3e41d1cd02b9bdb656e8a7f1a4f2a4279e2b105d4dd9cd385e5c0e923502b9b62203a827ea69c32bc04c0fad627dbee78fdeb63eb4a2fe7fef9a00d92c7fbb4c2c7d2b306a32f7c7e54e1a8b13caf14845e2c7d2a176f6a87edc87b1a5fb446dfc58a000b7354aeff00e3e5b9cf03f955ddc846778155e4b69657670cadec0d302a8ab76e32bef9a85ada6542761fcc1abba0f9c9a86d21d4321e08233fe7142436f4145bccc462190e7d10d4c34ebb38c40dcfa903f9d6e07dc33bb20d2f9880805c027a735b7b331e731d747ba23398c7b16ff01522e88f8f9a7507d00cd69fda2219fde29c7a734a6e23c9037363d14d57b342e7673bad699f65b68e60fbfe6dadc63af4fe46b1fd2bafd7e30fa5cc7fb8430fcf1fd6b901d2a6714b62e2ee28352a9a82a5435051ffd3e28c92375e3f1a72c13b8cac6e47a8535d0c70c51f31c6aa71d40c549bb91ce695c0c05d32e588cc640f52c2ac268929fbcf18f71935b05801c9a703d7b5176332468673cce07fdb3ffebd4aba1c381ba57fc0015a4082dc7ad29231eb4aec0cf1a25be4fef66cfd47f851fd8b075f326c7d473fa568839efd4e7eb485875cf5e28bb028ae8d6ffdf94ffc0850ba3db02325cfb16abe0f1cf1c7eb46e19c03faf7a2e053fec7b3233b1bf163c528d26c87fcb1e3fde273fad5cce013499ec3a0a40561a65a60fee138f5a71d36cc83fe8e9cf3c2e3356437048e734bbb9c023f2a00a8747b23d2118ea4ee34d3a359124f94718e30c7fce6af170727d4d359f923a7e14c0a2745b5ec1c64766a61d0edcf49261f423fc2b44b020e467f1a370ec3b73c52bb03306830923f7b311f5193fa507408ff008677c7d2b5377cd8e3fc68046d04019ce3a51760639d0307fe3e0f5feed29f0f92702eb07d3cbcff005ad72d8c734a1b04f1f951760629f0fb6de2eb27feb9ff00f5e81a0371fe94bff7efff00af5b6a49c81d7d3d7f0a427e5f5c1efda8bb0315bc3edc95b907d3f77ffd7a4fec0931cdc203feed6e9249e831fca82df3727f1a2ec2c617f6036de6e577771b334dfec1971feb90ff00c06b7f23247f5c1c527054e401ebe868bb0b187fd832647fa427fdf34cfec2b8c91e647f956f1395cf71d7d7ff00af4a5b1d7eb47330b1cf7f62dce0fcd167f1ff000a43a25d7f7a13f427fc2ba1eadd7ebeb4700e32083d3068e660738744bbf48b1fef527f62dd7fd32fccff00857464e78eb9a550303a1039e3a8ff000a39981ccb68d7833b62538ff6a9ada4de29c0b7cfd18574fbb3ce707d"),
                    Utility.HexStringToBytes("801ac9e76c6b82a24dce086400001bd04132381f0054ffff85030dce3af5c7f9e69f330396fecbbcff009f7f6cef1fe34d6d36ed5771b67c7b735d59c6d05719f6ff0039147b8e413f9d2e6607206d2e075b79bfef83482de563858a427d94d763c03f30ea70783cff009fad2329f9b8c8cf6ff3c53e6038b23071bb9f7a073fc42bb32028278fa8fe9f9d364860914ac912360e4ee03147301c7e1bd47e7461c73b0e2baa7d2ecdc11e4201d7e5183fa74a8a4d12c88e2364279dc18f1ec339a7703992e47506944b8ade7d023c9549e507ae1b0703dfa540de1f982b32cd1be0e065719a77423316e4af4623f1a992f39c9623f5a7cba45d267300600e32adfe3555ed5d090d1c898eb95e28d00d08eed41182bf435723be242ef8d5c0ea76e78fa8ff000ae7f61fe1707f1a50f2c67b8aa52627147427517ce638d703d39a9e1fb7dd10d0a0dbdd83ae07d7935cf25e927f78037b9ebf9d5b86ec160cb294707209ea3f11cd5a9bea4b876352f34bbe7b1b89279630150b6d5c9c81cfb7a5727d09ae98ead76b6cf1485648e4429b98648041ee3fad73d2dbc91927195f5155269ad184535b915394d32941e6b328eb81c9e7ad0c493d3005333b48f5a5393cf4ac8a1e0f1803da80c0672718a457ce00eb8fd68c6d6c1c03400e624f183e94aac074391d78a61048f9793d739a0719dddbb5003893df81fcc53b77503e94d6249270703ae2856caf5e6801c0f383c13eb4a5b23233c9a631258f181d3e94bb8703de801c080682491c67ad34924fa0ed9ed499e3a8a0078c0c671d78f634fdc319071fd2a33c92070befdbd2954e01e4673cfbd20149ec4633d68ddd71ce29adf4edc8c5341e4f20e3bfb5031c09db9c7ff5c53830ce3af14c6ced18ff00eb520e1b9c0e7a1ed401206279e7af73cd05f83fa8a667e66ef46e3819cff5fc6801e4e48200c7af6a507040e83b7b546241f29c8ebd694fde3c0f704d004b920b0207d2807d704fbff2a8f76dc7a7ad286c820f4ee3fc2801fb8ed3c1e3f3146e20e72067d075a8f3f2f3d47bd0082bc751d7d68025c9519c0c0ec29b9e71d41e462985896c82077e074a4248f4fa0e38a00933c0c93814bc15c3753ce7b1a8b782777233dc52a92b9e4fe9400fce579f5e8687624024f43d7b7d298cc41240c81edc8a4073c7eb9eb40123b639dbc93d3b1fa1a42dc7e3d4f6a8b7018c67d7ad2861b704f03f4a404c5b92412013f518a69620f20e31eb9fcaa2c83e871dc77a524b160464d004aac738e718edda9524c703827be3afe1500fbb8186f6ef406e80f071d718e280262c071b8633c7b0a50c57db38c7afe750863b71ce3a70339a52db94ed3c0edd87f853026dfb32a3b0eb4abc0190760007ff005ea0c83c7a7bf7a15ce57691bb9ef4089f714db938f46ec3f1a787c050ebfc59e7f9feb55f76541381923a7434e2323e6c8cf43d680278c0753eb9c93ffd6a4ddc9504039edfdda89986e3f31ce00191d7f1c5383827691c85e49a603976b1c918e4914f110703e501b39c7f9e690380c0e15c0e01009efe9fe14c42a141182231cfa1cd160209f4f86600cb10c919271822a94ba14441314af1f190a791f8e6b5d58ab67396c64e38fd69f20575f97ef018c74383fa53423959f48b88c9fdd895473943827f0aa0d11562012187f0b8da6bb8d87e765e8081c7503fa77a867b582e130f1ab7381c0e0d3b81c724d2c0dd4a91d8d598ee6373f30dadea3a1fc2b46e742658cbdbc99033f238e3f0f4ac79ad9e23878da327b3743f434f40259ed1586e52a323823a1aa52c4f0b61d48ee0fad4892bc4719fc2adc7224f90e47cdd41ef4fd446cb0c739a71f99381c53412c33d31edf952efc1da41e0f6a82814ede0f5f6a736493edc1a8cf049c71ed4e0410719e9da801e8d92077e6939c64d35720f23f3f5a18ee5c8f5e78e2901296183eb8ed4ce411ffeacd11800f6fc4d0ec00033db38ce6801c5861b9ce0fa522821b9047a8e942b673efdff00cfd689093ce723bfad00389fbd83c8ee"),
                    Utility.HexStringToBytes("801ac9e86c6b82a24dce0864000021604132381f0054ffff29809cf208cf5a58c1cfbe2918e5474f7f5340c90900f50303b7ad301fc7d7de923e08e727a7b9a5246d3e94843b2318271fe1ef519e5b3bb27d690151820f4e413daa50704f1ce3383dfeb40c030dbcff003fe54d6383f78600e7da987193919f5f7a72b0080824e3f4a00721c9e483c704f4a0b7cbc7ff00ae98f82402303d40e9426327f223d680154eeedd7af1d69e18000e3e9e94de0ab63b7e42980e7a1c8ee280243cb9c67fc69439047cc39e326914fc839e0704ff004a463d323231d3d2801c0eec8c67db348391d0f1de9bd0a923a77a729e573c1cf1ed40006033c741d051bfe5c839c77cf7a8b383939f63e94e04e7af27fce6801ed9eb819c76ff000a376477f7a6e415209edd7d6901e71923dc7340126492194819efe94849c64773e94d5cedc1ce3a9e29a486f73fca801e4e79009c0c6476a5e841c9c7d3ad478e38cfa641a556c704e0e38e2900a4e307bf5c1a52475c8e4f2a4734de1b006d38e7d0d1b77200d8393c022801dea768ff000a463823ae3b1f5a692791dcfbf5a771c020f03a6320d300ce0eee55ba93d8d3b39071c81d7de9a303033927d0d3771c1e7a9ce08a00909fe2ce7233834310d8fba7f2c8a6ab10720820f1d7a51bb8033f28f5ef480783f2eef43c7a5395d87cc99ce3271d0d3385383953df1d0fd7f3a1beff00cb8c91c907ad301fbb71560003d80ea7f0a50c0a8504ab13df8cfe3516edbf373c76ed4e0d8c907728c67bfa714c097ccdc18a9f9b39c63d7ffd74eddbdb19c01824f539a889076b1e98fc6804ed073b8e727d47e0681165be642cdc8c70cbd07d452093707604718404700d40aeca03a3721b1c0c1a95b9662304a9ee319e29889e370b8e80a1009ce0d48c55dc4ab8dc0fcc71d7ae2abe496f94ed738c64f3f5fd280fb8657e5ded9233d053027552429009e39f424d45736f0dc26d28ae9807079ea6a5120c676f04f049e73fe453c6d0c791e5b1079e0e07e9e94c472fa968725b92d0e5e33fc27923e86b1991a363d4107041ea3eb5e88f18e55c72727247e23f4ac5d5b465955a48815914f271d78e845303ffd4cb560a40c52105464f4f6a4db824ff002a19b23a9f5a4317702a7927e94b19c373f2fd7b5317af4c7f4a6b9dc473c7f2a4048f20ee7ea33d2951b8e4e3d69a8db547603f4a6b6ede724803d0d003df1bfb7b63f953d1be51fdd5fd334c183c6d19e878ff003cd34b367938faf6a007b6edc4743e9d734e0dc7d29a5549e87d7af4a6ef3f7474fe540c7b90af8190476a55da5724027b7a9a68f9b07b9edea69378048cf1d33400e39180840efed40391f363ebe946dcae492077f6fa5231d87819efcf5a4039c80bcfca47f9c9a6862cc37127dbd6932581c30ce738c6734608008391d381d3da8024e074e9ea0ff2a8cb1e9b980fe468dc4f40403c73d076a5c36776738e3eb4301c06465b24e7079eb48cdb700120fd78a4dc5491839c601a4fbcab818c718ef4002b6e6e724771520000ebd3a9cff2a8cee538ea41c714bbf041008fe94009bf820371dfda9f8054f4e3bfad33e604fcdd3be282db08c83d3f2a007b10067001e3a7614de49c1c7b107ad0a49dcbbb6f39e475a369e871827818a007646e0c00cf6148f8c8c67ea2a32d80474cf7a5cee1d402380290c50dce7f8fe9d69fbb83cf1dcfad3303180d9c75e7a5264e36f3c7e3400e6fbdf7707d451d08e483ee38a6e339ebbb3d7b51c01c1200e99cd003b760f3f80a187ce413c67a8a66e3d33c9e87229c02b0208031d4e280149c907391eb8a52c71918dc69300a9c2803a0038c53377a962bf81ff3d68024638dc06719e845378041c63bf3da80d9ebca839c52a96ce3243376f6a0042c020fe74e2d90cc36819ec699807d149efd01a0390c4e39fae41a0093072a7a9efc62901246dc60fe7e94d07d4707a9a0b751c11f4cfe54087839dca73bb8007229c3a818071c9f5a8b386c838c0ef9a37108304fbf7a6048b9c819cfd383"),
                    Utility.HexStringToBytes("809ac9e96c6b82a24dce0864000026f04132381f0054ffff481b0719e1874231499ca9618ebda82795e79c753eb400e0c31924851c53f6805b772477fbc076a88825429c03ee38fce8520e53680c4f0739cff9340126ec12571d09f94ff4a914e0850393f787d7dbf1a8b25d89e0f1dc75a7249c120023ae1b0476fc4531132c980801051b9c6738eb4e472a55f8da320f383eff00ceab2315249c9078cf5ebc9e6a556eaa51b6800291f5a7702c7982351b4038041040cfe1d8d4e842ca533b9540507b727bd53040493700a171bba0cf3edd4d488320861b4eecf3c8ce71d7ff00d754988bb9f31594e558824853cfb63d6a53fbccb003ab1c0cf5edfe78aa81cab938d8103753c364f1fa5588a45e8003b4052c07e7f514d12cffd9")
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame restartFrame = new Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    restartFrame.Add(interpreted);
                }
                
                //Draw the frame
                using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            //Sony Camera DRI Test

            jpegPackets = new byte[][]
                {
                    Utility.HexStringToBytes("801a9e7c000011987b36807900000000406328170040ffffce4ea383d867eeb0ee723b7d3f5ab3dc64eeda549271cf3cfd7a7039e9d7141d0590012080bc1edd0f6038079c76ff00f5d4a002411db047bfa803b9fd3b76cd0048a00ce47276ed193b474f9483df3f81c53fa161cf6c91ce31c1181820fb134077ea48ab9ce376463f87ae73e8383ec476a93190381e992473ededdfb8a0527cbd0930cd81b47a631c93f9727d78a7a8da70411f302071d811d0fd3a76a0896dd751eab838dbdc371c7271e83d2a4036e0019c91dbaf1d0640e739e99a09d13d16ddc906de39c609e72a01e7d33e9edfe25fb09e7fba48e3bf1d3a720e3f4346d606efd2de83d4741b0f19ec01e7a64e46083d0f3526dcf0173bb1db18f7ebd78e3e94123f6e33c9caf2471907a60301c719f51f4e4d201c9c29db91dc0c92b92405ebf303fe450efa595c05c67e5d87f3e99edc7048cf3ef4f8c75f900c11f788e0e71ebc8c29efde9376eba80edb82405c727b8231d3039ea48e9ce4538201b41c77c118386ebcf070d8ed9fc3a511d12bbb802aedddc1ebc7ca4679c7183914e08370c1e323a72477ef8cf23af4edc51f121f637ecc630368ed83fa720f4fa5757663e61d3afd31c9e4f2707aff00faa8b257d03d11dee88bf32f043027bf03d723d7a63f5af4db6f92c6e5c9c058243c9c7453d73d3a7ad2d6cadadfccd3e2ba5a2f4ff827e17fc75bafb67c51f13b6ecedbb31e58648099e87bf51f4fcebca63c71c81907d7207a9f7e3fcf5aeaedf21c7645c8463073dc8e99e9fccf5ce7f3ab6e3e5dc186307f1f61ea4f3de828a8ca0a9c9cf439c8edc6473c1e7fcf5a85bb0ce704638073ec403c3671d4e3de81a57febfad481f9e9c1e7a639f6e3819ff26abb11cf03b7a7ebef9f7a0a4eda5ed6effd6ff32abe4f400107ae17ebc67b63be7a9aae4e38c63a0f51ee0fb9c71cd05ab3b36ba89271900e381d404faaf03eef1eff00c8d66cdc1233d091dff0c647cdd7e99ef41565b5b43f4da31bb18cf6fbc323a1c0e7193c7e556141000e3a91c6320ff746187f9fceb9ce7271c6464e0e4614119206338ce49e6a552467033bb03257af7c7cbc8e71ee280255ec71c8ce7924007838fc7069e32718c1e381b403ce793cf07207f9c50175b5c95576f7f9b9e3a1231db20f3ef521efcf420671d318fc78e2825cad7d4773ce19bb7c9b792471d89c139fc6a743d1401bb1d1875f4cf3c720f51f8d0676dedaa5e43864b0e01e87b74c75185e0ff875a7e3201f52067b1c74073f79bd4e73c5026eed928c6471dcaf0073d0e783f29fc7bf534f53f2fcfd7057e50368c63ae40c7ebef4301fe8b9f98e3a0ea476c77efd8f7a979efce31fc3c7b06e33cfd7ad0215542f6cf20fd71e98fba79e87f3e29c3e52300f5c1ebc1e08031d392091ff00d7a2e0295fba71dfd4f1f4207a53f1d39c8cf61d4fa9e79e9cfe752dad535b00e03ef0f9b827f85b27dc8cf3c81c678a5e4e061783f4c0f6f7c629735b44b5f243ee18c9c1183c71ea3a7393c77f5f6ef4a072a08ee38c1cf1c10011907d7fc8a22eda3febef11d0d8ff00070383c6e1cf6e300703f4aeb6cd704678e0f1c9ce3b9c8f4fa75a2f6b24bddfebe43f99dde88b97000e38e31d33dc63a1c9af4294ecd1b5020818b598fb03b0f5f4a56bb693d0d63a475fc4fc15f8a5299be2078a65670dff001369d73d46338c0c75ae21158e481d3fd907f13cf4e6bb5ff90e2ac917a2daaa49233cf0a39e0f4e3a9e6a5f330b93d0f418ebdb9c8e79a43206604638c71fc239f623b5573c02338ce3be3d46473c3707fc680216e41f51927a027eef20938c7a8fff00555663c7cdd327380067b67eee323ffd541a5a364eccad20ebc10324f4e4e3b8c74fd6ab16c647619edcfd78a0172a6bfa4432484e7241fc07a1e4673d703b71fad66ca49c36491b89e9d7bf1cfaf6cf7a0d37badcfd3b5e58376c8eb9f94e3a91dbf3c55a55cb0c8cf239cf5c64e07a9c7e58ae739fe64ea71d88efcf423d071c9ef8c54ca3960c3b60e0e091d7"),
                    Utility.HexStringToBytes("801a9e7d000011987b36807900000598406328170040ffff241c64fe14012fcc07cc1b8278da067191800bf1f293eb4f5047f11c0e39507d7007a1fe74112d6eeff0b2551d7a91c8fa7a6ec9ebd3b77a900236e7380c73c74edc7e1ed9a05adda527a7f5fd68483918f986181e9c76e4e073d3d33f853f9c81ce411b4edc1efcf1c1edc1a081d8e9827193dfe538fa1f6eb522a038c7b2f00e0939e4b0e8681128e3eb91d4600f6381d7079e9dea45e181c8ee02f1f376ce0e73d47e540120006e4c11d31f7883cf46e7d41eff00e35260fae7207f0b76ec79e0fafd7f1a0071ce32013c01c1232b8c609ea0edc7f8d3f1d4e7bf1b94807a7276f51c7a75a971d9db5f2ea03b68396500724617a7b9070083d7b528edc93d08e3afd7b8fc4fd28e5d9b6f401e14f4c8dbc8c633f8afa1e7fcf5a5dbd32c4f7efc6df4cf51c76fcfd0e5b3bae9d005c6323ae3be4fd0018ce7fcf5a55555750c4f51dc1c1ed8f4e40a2daff35ff003a1b21d00031c71d988c1c7cc383d79aebacd727273d4af41c76c807a1a5ca9ad9a1ec77da2282ca39e31fe19e9c1f7ff00f5d76da9b797e1cd55cb7ddb298f418184e9d3ae45528a8ec5c65bdf6febb9f809e3894dc78c7c4921270fabde77e98976e707bf15cfa8181d7b8c673c9eed8e9dbe95d25c7e15664cbce016cf4ea3a8191c7a62a4eb9f976e093cff0031efc50323dbefc6723ae49f5e9c1f7e698475c0e7241e3b8efd792281959fbe4f70391927af23079edc5577e36828724e7bfcddb006d183f8f7a069bdb74579070c3241c9cf6c01db8ce4f4ff003c5536e324139ce0fafd4a8e879ebd3d7d282a324959951f3ce09c8e3eee73cf619e3e954a4e09c8e39e9f2e318ec1bbd05a77be9b1fa8318c9e41c163cf193eb9079cf5e3fad5851cedc76e7e6eb838e41e73cfa9fe55ce604ea3a37270d83d393ea00e8339ec4fe153853f37231b863a0c9e9d17af6cff003a01f5d4970063ae720638f980ed8cf041ff003dea418000c67e623a73f4e7241cfe3fce821cadff000094600c72579fe1cedeb9c7cdec7fcf34e0c148049e848c82380081b8f4eb8fc3d2821e8dd99fffd0de0cbc019ce718c1e3be327a7ddfd2a55c8eab8f98751bb1df705ea0f7031fe140127ab02dd7193ebcf7c7b74e6946dc1fafb027db20e3248ed9fa5004c3a05c609feef6f739a91557b818cfa8f9781c8dbd7273dff3a00973f3038c121474c671fde05467f2f7f7a914e08039da40fba39049e0fcd80303f4ef8346ba6ba00fc02c401bb24fca546471dcf75ce393eb4f18c1e9d0f6e72a7f3fd6a5b5b5fa80ed99c6e238246eeff8fcbd319c76f7e94f55038c0eb83c7a679f6fc3f3ed4f5e8c07151c6073cf1b72ddc9ceec9cf43d29a1718008230472a78393c371f29eb8a495a2d3603f1dd8f523e5c71c738fba79ff003d28dabbd78fe2e99191cf5386f5e49183f514a3ae96d62074364b8c7076e7d300e38c0279cf3fe4d76164bf7480a01206700f4ec7078eff00feaab7e633bfd1130e01241e83e5fcf2727fcf15d378a9fc9f07eb7264aedb09b248c71b0f63df8142f5dcbb68da765fd6e7f3fdafc826d7f599323e6d4ef4f23afefe41838ebd2b3e31803a7e1f8633e83df35d2f77ea5c764591d8118ce33d307e9f8638a0ff00c0bb678ec3b7bf19a4310e70460e338c71c1ee4e5b8350b0e831c724f3d3b640ecdc76f5a00acfb46738e3d4743ea30bcf23d3f5aad21ea304104e4e0003f878e393cd03daeae5693924f5c77c741c70c3b74aa6e40cf20af1d07523d30393fe4503493bf468a8e473c0c8c0c29ce47be47273ed9aa523019c9ef8e99f4e0e392682bdd8d9def7febfaec7ea0c7f3647cc1703e5c360fb0ff6bdbf4ab4855b2486078cf0003e81700e381d3b639ae732b59b64ea71839257231827232485cf6c71d8f1c54e3918c3647d7f21c70700d026d2bbbff5fd22753804038e41e739efb48cf4efec69ea7a64639e081fa0f4e68336dab92af41df0c40c8e4739232791fd3e95201d4e7d4e707af380c4"),
                    Utility.HexStringToBytes("801a9e7e000011987b36807900000b30406328170040ffff71eb4123f073cf5c927f5f53d7a7b7f3ab00b0c67236f3d7000c6371c0e38ee3f5a003a0fcbe5c74ce0e092dc1e7a548bdb9f4fe1fbb8e99c2f5cf5e4e7a838e680255c0fe20304fe07a6401df07f9f15285236f1db903824f3c050b8fd73400f072b82090086e9c8f503d3e9c81d876a914119382a4e33c9c1ed9e17d3b73f4a36024fbb91f3fa0e1c657af1c1e3be2a4db81df68cf6e0ee18cf5e3e6150fb35bb0765bb15546dda473c61876e7b63bff002f5a9318c8c7461cb0e84f73c63f1ef8a5b7bb6f40ed60e0f383d863e9dc1fa0ff003d29c07cca7e63c9ce4648e718f6232714455af776febcc00e3ae7ff001dc118e847afb926818e3201390470369c1ea73f78f1d334e2dfbdd581d2588fbbc7cbf2b63278cf047cc38239ff001aec2c54700e76e075edf5e7ae7f9e29a57b3bebe407a0688318e4e738f9b827dfdfdb8c0abff1166fb37c3fd7df2462c27e7a13fba7e87b0e3f0a717ef256ebdb434d22b77aa3f00efa432dfdeb807f7977712678fe295ce7d7b5310e0633dc7f0f0075e3b83fad753ddfa971d9684ca76f4cf24e323f32076fc7ff00af4a71c673d7b8c723038c8e3fcf5a431ac3d46724718033f5c8e4ff002fe71b743c907af231c039c74c038c6322802ab7009c7239db8c73d0123b1f4aa6c40c6e278cf4feb9e41e07140fa34cab2b7ca7e6e39edebce0e7a7359eefd8678c8201e9f975e282fb2e56fef28cae093f374c8e9d474e7d07f8551793a9dbb874e075e08c63a9a02364f6d7f2fbcfd4f52c30c029193ce546719c8ea7e6e7fcf5ab51e73918c9c753d0f5c7dee783dbf2ae730da56d5bf52652dc64742474c6463827774e9ebcd58dc406185e08c71c9f51c2f439183da821dd37d0941385e48dd8e739527d1739038cd4aa0e78c6ee0938ea083d3fba78fd68249149e4018ebd3773ea475fcb1d07d2a4524b6d5ea7e5c80704e3a938183923bfe14012af50a19bf88e3031c73c819f5efeb4f53c8040032719057181824e7a8e077ea680245e1b38f43d3240e4007040393ef9a900ce79cf27777dbf98e79cff009ea03696e4abf7873cfb9c67a8c600254f1c75a7ed5e01206473d723dc6d38073c7e3d28174d35250b84036e339e17d474079182393d33cfd69e870a092dc1e491827b76e41dc39eb8a3e4473356bbf95bfafcc9402032827ab670cbc8e4f0abd38fe7d334f8c0627e53c03c01f77070493dba7b9a4ecf46f629a4ee9ebf9922aa91ce1b924f38c1e783b80c9e3aff00f5a94824f4539cf00f18f4ee738fc6a1dd5aefaff5b82f26be5fd68c90ab0e8065719240cf6e0e4f5f6e734838e3ee82c3ef02318f40a3f5e6946efa7e1fd7ea520ec01391cafcc4720752463ae7f2a178604b639dbefe80101b27a9ea7e87bd5ec928ab81d1d972cbc7f0803006095238e84e71d39eff0085769601b8381c618fb7b3607ca7f0eb4e29adddc0f41d0c1c0c01d471b71f8f3ed9ec6b3be33dcfd93e17788a4c818b09f270b91fb97e47bf4fce885f9927dfb8f449b6cfc15762d24871c995bfbb93f313dfa54cbf4e38cf0338f43edfcaba9eefd4d63ac56e4ab93804ede838c367b8ce4f1c0a5eec39c8c8e40e79c64fa7f9eb48a1a0f0783db93c13ec38efcf3db350b6460ed23f2e31c646381d4e0fb5005493ef0e07b93b463ae07ccbcf7fc6a9b9ea38c6464f73ce4ff09c7b1c8edd68295ba94267c0e41edf7ba73d011ebf8d67cb267b0e72738c03ee320f5fd280dad6e867c8fc13cf73d338ee4fb1cd5091f8f7c9390393f4eb8ef9e3fa5038b77beecfd5a8f3d39e411f3167c81ce3dcd5b50a403b71c9e769ec4e323f87ad616df439afabfd4994f2793ce3a03f37a1031c9fcfbd4e33cee3cf1c953c9071c8c6734872eb796a878c3719c31c76edd39f51803fcf153e3a67f840e83ae703231d4d040f519c952095078248f7c8f9b240c1e302a5552413eb8efd781c803db3c718fc280255ea0a9cf3fdd278191f31fcf1ff00eb353038c6"),
                    Utility.HexStringToBytes("801a9e7f000011987b368079000010c8406328170040ffff4ff08e8766791c9e79607f11400e55191ebc0e9c9e70402df871dea5030c41e4303f7471c120f233ce71cff92132766ae48171900f19f4ec4f523a83edc9c67b53f04e071c118c638273d71df8ff00f5502e68a93d7f02c2f1b403803a123d3b0eb91ededd28098246796ddd4723d8fe2476a0cdb6f73fffd1f43e41527927a8c03f32e41e99078ededd6a655380a339041c61781923d7a01efebc76a3e465ccfa2b12e32cb924ff00c0727a11807773d3d4fd3229c17a725baf24739fa8ec79acdb4da4d356ee559d9bbb7e8c0216e548e8067eef383cfddc83c74fff005d054f3d7af46c1ce39c75e4ff003aad2cff00cc719746f50618c6461704e727b71e9c0ff3eb42af2a7271918e78007738edf9ff00314edb59ff0090dbb6a749629d09c1e40c01d0752338e4f3f5edd39aed2c0671cf4e07d78ce73d0f1eb4bd55be7fd7dc33d0f46521bfe04bc60f3ec41edc1c5711fb475e0b2f847e2072c17759dc270464929b4003b1cfd7bd382b4959e97febd07f23f0d1473d8f27a81c1ec3af1ffd7a9c70070dd4ff000e3db23d7d2ba4d96896a4cbd8739e01fcf8079ff3ed474031fe7dcff9efd68181e7827f1efd7a63bd567382df30f6e0f1e84f607f0e2802948719e40e473b8f3ef9c71f4aa521ebf312324f5ce3ebcf1d3d7fc68033e76ea3dc9e011bb18e060f07f1acff009e662912bbb647caa3a1f53d81fe74149db4572dff00c239ad4e81a2b4775e9852727d4281d49ef5cf6a1617ba7398efad2e2d9ce70268dd33fee92003dbbd68a9f35f95dda1f338a69defe67eaaaf7e7bff000904329c1f9815c03d323a55d5e0825bfefa38f4e011d08c715caaeed148e5274e727391d79edf8139cfb7f3a99540c0c03c8ef8c8fef1c74fcf81c71d286bb3726165a9617a118e84f6e48e38f7fbdedd69c3e5c8c0e33cb019038e703bee1ebc60fb54887f1c1046727381c9c763ce47d7af7f7a9b38e01e727a8e80e0e41edc8fc2807b3b0fdd92412c739e0e4f03b9e99eddfb7d6a553d319e08ee0f1fed0fc3a7f3a03e64b9c93f8633ea067a28fe4475a97a606480406c1f9b033e9d8f1f5e3f0a087ab6ada47b0e191f78b37240ce38ebd003e84e7af078f4ab0b96c8c903273d0018ce54e7a7238a0cdbbb6c90630473c827f847b7cb9eb8c74c9fad3c8ce781b9f182091f74738ce46e07391eff0085021ea47cca0038c38fbeb8fef0076f1ce7b63f0a981e00238c0ea0707d33d41c8f51d681f64480f0475618038e84f40c08e4fbe29f90413d70002720e48ee319c1cf6a0ab6fafc3fa7cc38e573f7b030caa73d4107e6040e07f914bf2e4a8271c638c63a104e3a639ee7fad4db75a2457345b4eec4395e33cfdee303238ebcfb7f93cd3872cbc1dc0819ce7203678046413d307f0a76b5bad8bf43a4b1c00b83fc4b8e47d08201c06e4fa576d61d81248c8c707e982a4723d727fc6876d13ea07a268abd383c30209ed939c0c70318c8af19fdade5107c23d455582ef42bc0009276fcbd383cf43eb5705efc3c9a07b357d0fc5e4ddce0e393dbb0edce4679f5ff001a9fae3d88ec07b7bf3ffd7ad8e85b2b138e318182bd49183df8f6e3b5213c118e33e8067b64827079ff002680236ea3a600079ed8c9c007d89ed55a439ce49ea71c01bb19c1e7a1f5a00a121ea33d3e831f9718ace964c64e48ebdfdfb75ec3da8032e47791d6246cb39e0e324039c1386c03f87d2bb9f0e6991c6d1ef4324b23aaa00b9777276800632cc4f4cd3f2b170767e6cfd3efd9fbe0041ac59c1aa6b9621fcd0ae8b2c7f2c60f65c7392bc13dfd2bd2be3a7ecade1ad6bc357074ed3ede3b88a26292431ee749155fe7e3e60e1b6e064fa71442a72cd35f64baf4fdc76d4f9e90edc1382300641c027d42e3078ed56d3e5c101874fe11d38041f9864722b08adeef4feba1c24ea490a403818ddc7ca3d0e474e87393faf153f5c10c79eb9271eb8193c1fcb2314e51e5bd9e8ffab0130079c678e09eb9cf7f98"),
                    Utility.HexStringToBytes("801a9e80000011987b36807900001660406328170040fffff00e7bd3871c13dc9e99cfd3fba71ea39faf15004ca3249e77617be0b608ebc71f28fad4cb9d80807e5207cccb81d3f889c91f81c504b6ded7baf21c011c11f74818c6d27d8707279fa54801ea46029078cf3d3b1ebd4f18edeb412ddefd6ddae4abf7416c819e77607e078cf5edf9f4a72e7036927fe039181ebe9d3ae7d2815e5ad9dedfd752751c8e3ea14f7ec38cf3807bfb54cb9553819f9b6e7033bbbe31f439383d3d33413f3245e18919f9b19f5cf233c2f00e38fd6a5da0a8fe1fae7af07039e4f03d3afb502150ed23393c90720e083ebf29c8f4fcaa75c8e3703f315042e79f4383c1fa9cfe19a007056e72bd719ce41c72b81c8c9c8edf8d3c29390aa7bf5ee476fcbaf3f5a0a575aa1dc0c64e075c608c6471c6ce79cf4e87f500e84823938f943038ea0e3a1c77f5fad02f514ae7a039e9c0e99c723d7f0fc3dc453bd720f0de9d73c7cc41e3a74fd3a54be6d7a7f5e85a697dad3d0e9f4f07e55e369e0007b8f5dd80071fe78aedb4f1d3208c9032c02e303a71d3fc3bd0b5d6eff4293777eebfebd4f46d1474cf73edf4e0f3e9fe7ad7ce3fb69ddf93f0c12dcb0fdfdc409d70a49914027f239fafae2b4a6bdf8eba5c6ff55f99f9091f6182707d38f4ec33d33f954dcf3807aed3c1e40ec7d0f5effa56c7447644abf7470c304f4c71cf6c0e0f03ad0dc0383ce3b81c9ed8f5a0640c7a678e98ebcfd3278aacc7afd58139c29e31b7da802a49dc74c63a1279f4381c1eb5897526dcf39c104fddcfd2802a698e249d9cf4dd8f7383d81ebdfb57d6dfb36f8065f1df8d6d9e480c965a74a87054953283920f60428ee3f8ab49ae58afee9a52579abed13f72b46b1b3f0d6956f616f1a466389158a8da41c018f6e7f9d1757d1dcc32432e191d4a9cf23d320f40735847bf57b9abd6fe67e632c4c720f183fc2777a1c107927d8d595889c704e3209098cf1d474e3f967ad351b5ecdab9e713471e707aee61f87aa807ab707b7ff5ecac4381b71cf4c631ee063a67afbf7a99745abb790132c78071c7a9cf55f718c6738fa62a458cf19ce17e6ea0823a1f71d33d47e5516dc3be849e51e472060f52067078209e879f5fc73522c639183c63afafa9048e6912a57bab6dfd7f5a92888ff77a63b6339e003c654f1f4fe74f10b647054e0751d39e9ce71f4141127696d6f4dc944582580e73b790d91df276af3db1f4e6a4f2718c0fcc743d081c1c77ff00eb71413dc996123033c8c7257a003a0e793d3ffad8a9442400707181c719e3b8e38eb9c7ad02261170483939041c73e841c8e46d3ea318c54cb131c81d0e187cb8c803ee83d41e71dbe94002c5904104f279c64861f778e7d3dff9d4ab167072392148d839cf19c15e0fa1e39ef400a23c6173fc409e319c71963df1c73fad486361d41e0f5c939db9ebbba9ebf5edcd0002203afa770720f424f73f9fe9522c5d391d47453bb838c8e793e9da8017c9fbb81c827ef2e4139ea71f77f5ebdbad3d60f9c1da7823a83c7d32bb88c8ff00eb5035a58fffd2fa26c22c6df9476e31dba71c7bf4f6fa576ba75be368033d38dbc1e01c7f873f9d0446edebb2f2fc8f44d1a2c6df94753d31fa6070dc7e9e95f21fedd17063f05e916831ba4bc84721571925b9007070055d3f8e3ea36ded6d9a3f2b238fa1cb75cf4fc32471e9fad4db31c63d0f00f3e80815a9d0972a7d592aa6dc67afe583d3f0a4913b60fe27af5c903d6828af22e41dcd9ff80f3c76381c7e5551bd88effc3d3af031c9248fd680336662bb8918e0f6e7dc9f5e8315cdea52feedf1d3a038c67d78c7078ffebd34aed2b010d866284b60fcfe8b9cfbb738e99f5cd7ed27ec3de054d33c2316bb7100f3eec79fb8c2413bf91825b93f374eb8efdeb4ac9c6069415dd57db4febee3ed9d62ef9619c0cf618c638c1fcab9b378707e6c81c75e47f874ac96965d8d4f815620718ddc13d94e0fa8da3a753eb5692103195c9e475ce4f4c823a1e9ea7daa63757bbb9e76c5810e"),
                    Utility.HexStringToBytes("801a9e81000011987b36807900001bf8406328170040ffff58824e7381c03cf7000278e9dea709c0206e5393c8270071cf1c1cf51fe4b96cff00afcc09638b183c11939e7dba1c7d3be3152f95904606031c71bbd79073c707918fcf8ace7d3525bd5ab3f9122c38c617239ee707b02403f2fe3eb5388ba70720a8e3b76e03741c7d3f1a9dfa129a8dd2d57f5fd6da132c38c601562482081eff00776f46c7b0e94e587819c71c9001edf51fae6911dfd4788d812406ebfddc67d864f4ff003e95388871b81ea31c74c67a60f3ce31c8fa8a0448222318e848ea7ff413f43dfd7a54eb1679da3e504750464f03036f278fd78a0099621c8e07f174e7b020fa9e0734f58f0dcf620f0070064f5238c73de801fe5e48e0f6273f75483d075c9c1f6feb4e11e771c6e009183d3191d307ef74e3afbd003fcac63a82c0e486ff0080e18773eb83dbde9cb111904839feeaf51e8723238e87a718ce2801eb0f5c86c8ee4601f73e84e3afe94f48b18ebd7f880ce7a0030011f81c5003a38b90704743d383c11d8f07824707ebd6ac241d3238c8e7201ce49c0e39fa7340fe674da7c0410319e79e339c8e840e873d78c715d9e9f0fdde3dfa7031fd7ad03e67d1d8f42d2a201d7030370c81c7b631e95f097ede73edb1f0ddb6719b956c7ddced89ce48c024e0faff002ad292f7d7ccbbdd2d6eee8fcd1894e00c0182327d7903071d38ebcd580a474e7e83823a60fa7ff5ab436526b4b6ff00d6849b76f638fafea78e7a71fe4d364518ee707d4707d4f391f9d068e4b62a48bc1cf5c01d4f5f4faf4aa3230e4ed6dbcf6c631eb9ff003fad0331ee7001f539071d874c723049e7fc6b93d564c6d507ab73cfa76c76ef9ff26aa1f1202ee9919b9b8d3ed40e66ba822201ebbe55196e3e9fe79afe897e0169b1e8bf0db4744428ed6509f940523e453cfcc31f7bd78cfb66ab13bc55cd28694a6edbc92fb91d4eb37782c73dc75efedf5ff3cd734d77c9c9c6323ae318c75e95997d5a6f53e438ad6419ca1f941c6006c377030783d781f955a5b46c8ea3a7246483ea70bfbbe9fe7ba8bba5a1c1f32d2daf46057a753ceff63903f2fd78a956d18e481bb6e7276fa7f06475f6cf4f5a61dd762716d20edd70464739524363238f4e3fc2a5169270410339ea31cf5249ee78c7e352e2b5d353193b92259c983db27d8e57d30477c7afd78a9859b80060a1518c118ebd481904faff009cd435a26debe82ec4a2ce43d01c1238230a49ec7f23fe1eb2fd8e41838009cf2ddf1d8f038c81ee6a044bf6361d473c8e83afa0ee3ffad4ff00b1c983900755079cfa64f3c76fce8192ada3839da71c672791fed63a7af7e2a6168fc92719c741bb9cf5c91c11f875f4a044cb68fc1da3233c7a63b0cfdd6fca9e2cdc71b54e42b0c0ebcf5278c1c75ff390078b5906cf954f0467fda193d76f5cd2fd91f208403279ca9e48c0c938faff0089a007fd91f0bc0f9707039f5e4e7a7e9ee3bd3859c983e9ce0e323e51c823a0fcc7e3400ffb1be4614a9c91f74f38c7551c03fe3522dacaa471824907e5e9f4fef0ce3a723140138b490e09ceec91d08fc47183c9ff00f5779d2d1f206ddff30e73f8e0af638e31cfe140ce9acad1f2a40c77200271819cf038e07e95da58da95da482381dbe9cf3d0f27a7f85016b599dbe9503061c7192791c8fa007dbffaf5f9d1fb79c8d26a9e1ab618c2195b03a7cb185c9f524b71e95ad2f8d795ca4be1ef73f3de281fd1b8f63f91f6ab4b6ee402339fc79ebc0e9cd59d09c7695f41fe43820800ff007b23a1e809f43c74e7fa535e093181cf23a03d07a81d4f1cd0118f35ddccd9627c1383c123bf1efedd055092273918fc71fcc7e1ff00eba6f5355a5ae615c8c77e3001e9827dfdcf35c6ea68ed2c31fb8eff00ae33ea055d3f8d5c3a33aaf0bd9b378834142010da8da0e4641fdeaf38f5cff9e95fd15fc3a430f8174851c6db280607cb80100c7cbd3a1a789de1ea6945da94bb737e88ccd6da7f9b691d475e3233d720fa8f7ae499a7e4718e3920fe"),
                    Utility.HexStringToBytes("801a9e82000011987b36807900002190406328170040ffff67079ac85f69e97d4f048d9339f4c31381c7b9e38edf8f1cd5f8d90e3f8707391b79f61f2fdef4e98f6eb59465cb757d0e34ef7ee8b086338390001d1b3f4cf2dc3559429bbe603a10395e7ea7d78f5ab6fdd6ff00afebc896f9644ca46480380c3b9c8c03c818ce31fe7153204ce189fc41c9f7273d876f7a953dee66eda5958b0020e84e47a1e8c33f3007a7d3f0f7ab0aaa7009e573c85c1ed9c7030381dff3ed37bab357b0899553904b76270a011db1db2781dfe86a6454183b771c918cecc0cf5c679f715204aa9174c0ea7d70391f80e871c54a228d71ce54f00118ef9c673df27fc4f4a009a38e3c1c8c05cf524e00e78f53d3fc2a75863eb8e493db07bf0320f3f8fe66802558138c13ce73c71c63af1c739fff005d4cb0a8e71eb8e07e6318cf4e99ef407e03fece8a339dbcfa738e47a63f2e9520850eec02467eee3763048c9e38ff00eb501bec396d90e3000008257807dfa9e5bdbaf14e16e8df3606ec750013c0c807d7af3fce8017ecc99c8e09c75f9141f503922a55b64190491f3678039f7391df9c1e9cf4a00916d90f420f4ea3d0f519ebfe78ab11dbc5b80c60923a0c7d01e38e9e9f8d00743650c5f29c818c6781cffbdcf1d064f7aea2d7cae32d8c1c631d3d31d71c0a00eb74df29581071c82471cf3db39e6bf33ff6df65b8f16e871aee65486e0fca70491b17279ed9eb5ad2f8b6e8ca8e8d1f10c7683a103ee919dbc1fa74c77ebfa0ab71da0181807ef7d4f3d03639fcfa559d516ade9f88f6b64eb80dc7f11f941ce32338ddd3d78aaf2dbe00e8d927b8e38c673d3fcfe140d4acd24b43ffd3f86e788727b7fba9cfd720e3b71c7e75913db925540e41c0e0807b8030323a7bd74ae967a9d073d791eddf95f507db9e7183c9db8ae3af230b3c4c7eeee0bc6381ea7079e95a4236bdd01d7787e48e1d7344949fb9a8da374e78957a73c75afe85be1acc975e04d21d795fb1c2c304f07cb56c1f7e9c7ea69577cce1a7536a69468cadd25fa7fc022d661e5b8e39f7cfb8f4ebf5ae64db0fee8ec4fcabc6dedbb6f03f1acc2367767ca897678cb15209eaa781d3a638e055b5bd27862bd33c11caf4f97d0f1efefcd44636f5383abd4b097dc0e7e5c601c633839c0eb83ebebcd4e9a830e371382588c741d39c753edcd292b5db5ff07f1dc9b3ed65d8b03530ac406c1040e474e477fae7183dff001a99753ce064f18ec403ee3d7a73fcaa5ae5da464483521b4e181c30e4a8c8c1e49e7d33f9f5ab0355562724707f8b68c0c631efd6a476b6e89d7535e49604700e02104f390495caf43f81f6ab0baaa7cbc85192472b8e0738f4c1cfb0ee79a044835445c1ddc606327ef7a1e460fd7f2a9bfb5e2e006e9838c8383c8c11ff00ebebf85004ababa123e7e464f207403aafa0e79f4ebe952aeae809f9c6ee08391c8f738e3a8c7f9345b742b3bfc5a13aeb2993871cfb72327049f53f4c8f5a9d7574231bc75e30383cf4ebc7ff005e936d6cae4b5adefae848bac271f30ebdb6838040047a738edc77a906aea7386041ce3246307b9cf4383fad0af657412bd92fb4872eb083f89770e3a8f9b1c0201e9c7bfe229dfdb2986c1c1c8ea471f89efc1eff009f77df42af17617fb693a070781fdde7f33c9c9e94e1ad21e72383dd87e9f37047a0a3b8c906b51f1f3773d860f5f4e87a714d7d7915c7ce3b16391c9eddbdfde8036acf5f46032cc31d79ea7a6466ba4b7d746402fd31dfd8f07381da803a7d3f5f4c8f9c704e7f3ea481cfe75f9dbfb5dea02efc5fa6124710dc7cd91c64a7af5ce3d7b56b47e3575dc71d1ab9f242cc833f32e073f2f6f4c67ffd63e952fdaa3c672b9fe1f9783ee78ff3f5ad1a6b747545b776e5a22192f9173f30624e303ffae0e78ff3dea9cda8aa86da79f461c1faf4c715518bd1db416964ad777febcfe462cba929c92c339ce577700f000e3dfd7e82ab0b957c8ce724fafe478e7ad16beb7ff816f99aaf52bcd08911863bf1f2f5e9903e5f"),
                    Utility.HexStringToBytes("801a9e83000011987b36807900002728406328170040ffff948f4f4fcab89d5ed1e221b195ddd429e793d78383c1ab8ab25a0c96d1590dadc039f226865ebff3ce4078e0f3f2d7efbfece1afc7aefc38d370e0b476b08c120e709b403b54e3af3eff009d4d656e4febc8da9fc1512f2ff23d1f588b96c0c7200e39273d2b9b307b6318e70063dfa75e3d78acccd5d5d27f81f9f89ad823ef1ea73db27d573ee79e953aeb0991927af1cfe181e9f99c7b50725b6b3b138d5c649dff0030231838c678c74e0ff3f5a97fb63a9ce003c1500600e3030300f2734acf5edfd798c78d679ff598e9ef8f6383cff5a7aeb60721cf523aed07a86e0743cfaf7a89436b74224be2bafcc986b43f85ba8c1e73939e87ea0fa9c7d69e35ac64ef3dfe6c75078e012403c7d47af350d5b4688e5beade9dc906b5c0c48fc803a91f97a739a91759e87cc2791f518edc0e013fce90ad6bea49fdb5b7f8b8cf623b77279c0e3804d2ff006e85230f9071dbf00a31c8e9d3340870d7b93f360938273d474dbd7d3a8f6a77f6e1e01908ca95ee31db9dbd7a7a9feb4012aeb9b7077edda4639f4f4e3007d6a51af03d641d48e0e7701c771c71c50164b6561dfdbca769dc3827b28c018e0f381d391efd8d27fc2401b2108dc07f16e504703279c74e9cfa55257ddd8022d7241c3c81b9cfcd86c63b0c75f7a9cebbbbab0eb91855f9707b73c723d7da895afa1318daf7dc69d7875f331cf50464f6e483cfd69175f3c9de3dc96ceeebfdeebdf8a9287bf884818f31300f505874ede83e82a83788b2c49933c9cfcdc8f61c71cf5ff2681fc8d9b5f11e369dc31c7cdb80cfa1038c9e95d1daf8a49c032671d307951df1c9fcbf3a00e9acbc55b48db201f30c9cf2391c9c8eb8af88ff0069bd6e39fc4b64e64ddb607fcdcaf273907a72315b515eff0096a528bdeda2f2dffaf99f2e49ad20cfcc3233d4f071f8e01ace97c4080b1322f20f0c436ef71bba9e38ae9e55d8d6fcba25fd7e86749e258d7204808e7ae3d3a67fce3f5ac7baf14c5d0c9f2e7a6ecfb739c678fc7de9db6d47fa19c7c4f13b604dc70304f03be0e3a7d2b7f4ed5965e4303d3b29ef8ce0f19e9dbf0a56b5f437845beb73a5b6983e0e4e3e8063a8edd0f351dfdbf9f0b02a3bf040c9c0c12d9e9c0fad2d535a2b2febe405cf0ce8dfda08d6abb73c8c10833fddc16e87d81fcebf51bf638d7decac1fc3978fb1a063180c7aa9c6d233d4f247e03344d5e2ff00ad8da97da4faafc8fbab54b0ddb8a8ca920f4ebed5cf369bd709c1fd49ec78ebef588d452e87e430d571c6f030464ee1c76e3278fa54abac0ebbb764f6c76c718cfe7cfe34fe47138bbe89930d5c103e7ec0f523f3f53f89fad3ceb23032c0f1c91f9727764118f4ff001a0566afa0e5d61464ee3ff7d28ddd3a7a9fc69c3591c82e7391fc40939ec71dff003c0a42b5ed75b1326ae07572464e70d8239e838e0f15606ae31cbb71e879f61fe78f7a4e29df4d49e5d1abff005fd74b8bfdaea3197f4e327273d40f9bdbdff1a986b083f8bd71f30f4ef8fe2ebc54f22efa8f955ac35b5a5195320e327ef0c7d48ed4dfeda8c6d21c9e4742b8f4cae5ba0fc3ad4fb37dc4e0b4b6828d72304fef01e806597248f6e7f9f6a906bd1e39719cedebdfa678e869fb37dc5c8b41dfdbf1600f379e99240fc4f5cf38268ff8482153feb0648e0ee5e4f738fe99f5fad1ecfcff00025c75765a09ff00090c2300c89f98fa73c8c920f1d7f3a77fc24108c6664fc0afe63dff00cfbd1ecfcc6a9f7627fc2476e3fe5a2e79e376083827000e87f1a5ff00849a01f299d770c77fc483d36b7228f67e62e4dafa7f5b7a919f145b28199d79e73bb0b8ee467af6c533fe129b60bfebd4f3d8af27d5b073fae68f66fbe857b35d5942e7c69650a12f703b039603affbc78fc47e358eff001034e4c03771a7bb491f3e8bc1ebf5aa8d2bf4b8f9108bf13b4b88fef2fa3e081cc89cfd0af039f7a6b7c65d121c07d46118ff00a68a4923d7e618e01f5aaf65be9f88e315db7336fb"),
                    Utility.HexStringToBytes("801a9e84000011987b36807900002cc0406328170040fffff688d074f88b1d4611b0919f3d0e31dc00c41fe67d0d7ca9f17be38e95e29bcb4bbb3bc12ba2324bb9b90470b8cfb01f4fd6b68536aefb0592be9d8f069fe22c672a1f1d7f8b907d0f3edffd7ac49fe2067a4cdce7f880e9ce0e3df15455ef66bfaff82644be3cdd91bd9bd8b73ffd7ef58f3f8dcb13b5c8c640e4633e9df1fe7ad3b37d02f7ea5783c6cc2601e4ea460923af391cf5eff957b2f85fc43e74699932095ce4f4cf718fc28b5b7474d1d7a9ed5a5ddef5520939c0ebd71dc10793c7a8eb5d5e14c60124f4ce074278c72393f89a8e5bdeeefafddf884b46d177c21a9c1a2f88a3fb5362ddcede72a833d8e7a0ff000e86bedef0278cb4ff000f6af69aad8c9fbb9022dcac67207006f2460633efc0fc6aeda3ec3bfc3e5747e96f837c5ba6f8b74db79239a3797cb4e8cbc9c0e0e3ab73577c53aa58f86f4db8bdbb9638bc98ddbe6703eeae727d06071dab9dab368aa92718b68fffd4f82478d4e010e3273d180ee32064f3f9d4abe36200cb74cf723db07079fa7f3af40bbbe97d070f1b80768753cf3f373c7af3c9e94eff0084dcaf0241f8b7afa8279f6a04dbee40fe3cdb952ebc74c9fc339fe11c533fe160720798319fef7a0ce793d71d38fc68b2d7422eefa2d0917c7e40389b0463bb75ee7bf3c53bfe161edc6263dff8873ee403cfe5f8d165d87af607f88ec9d5c1271cee273fed1f418e9f4a88fc4c0a083364646467a9f7ec6972a57d09f7affd6be7f919779f163ca055a75ec701b92793ea7e6c0e7f9d7352fc6711e479f9e4f61c104fe9c51cb1d7404f7badbfaee517f8e08bc7da3d7f8f1b893c93fad5293e3b05c85b8e7fdfc81ee71d4f1fca8e58f625c96cddd7cb4febd4a127c7bc6e1f6a07923ab0e9db86ebc55393f6803f37fa4e3078cb37718c0c53e55a683934d3d59037ed047a1bce0f1d5b23e9f9d427f6823820de9ec3efb71dfa669fb3b7d91a94744ba95a4fda0dc1e2ece3d37139ed8e7fcff3aa52fed0b2f5176e40c8e1b3eb9cf7e7d01a6a9f921ef7d3e133e4fda167e82e24ce4f19393e84739fd7fad664bfb41ea1ced9dfb8ea79ebd0eee323b53f67dda1736f68bfebfad8e3b56f8d9af5ec8ccba8cb1a1c008a7ea3030783c57352fc52d724cb1d52e37640fbedc01ea074e9ef55c9dbfcc2f77cbfd7e0cc79be23eb72125f52bbe720fefdc1e38cf1d38aca9bc73a9c992f7f727927fd73f3f4f9bae4d3e5f37f9095d79fe6655c78b2ee5077dcccf923ef4ec73d7ae5b83d6b325f10ca48f9df938c64f3edcf5e3e94f952d914b57e4556d7a5cb7cc49e7277703b6707aff9fad52975c7e4961bfa6739f6e83af1daa5257ba7f70f449746537d724c7de3bbdce7f1f6e87bd566d665da7e63fcff0011f974abb6e16d36d4ae35693925cf7efd3d3d71f9d7b17c3cf15cd0ca96f339f29d9546e27e5dbcf248feb59cdecadb1d1415adaee7d85e1ad623b848f0d9c63182dc7b9e38ed8af51b6ba464186e00c7527a8ea70b93dba8accd6a5b4d0a97aa1f0c3e52ad90f9e87d46381f4cd753e15f1d3e90e2def24c47903731e1803ebfc2dc7e03da9edb3323eacf007ed250783940377bed146530c59a2e3380aa7e61e98e7f9d723f1bbf6bebff001869efa4683248a24531c972a5d02ee18dc3272ce32703d79e3a53a705cea72f862ff214d39a51be9b1f9a23c790af3e70e3df0476c73c03f8507c7d18cfefb1d7049ebf4f9b93ef5261cd25b2d1f6213f1062500098738e30bf9e4139fe755e4f88712823ce1dfbafe6412718ed40e52daeff00233e5f88d11e3ce1e9c11b401cf427d40eff00e3544fc424dd81718c93fc6bcf418c13d78f43410e56564f72c47e3f5247eff238fe25fae4fcdc9cf6cd5b3e3952b91718e99c9e47e23d80fa7eb409cf75d8acfe38e73e76ec7b818fa7cdcfd7dab32e7c7593feb989c8fe3ebe848ee7818ff2682799eb77b9ce5d78c259720ccd8e40c36318e327d0fb7e95833ebecec7f7ce7aff0010cfcbc7"),
                    Utility.HexStringToBytes("801a9e85000011987b36807900003258406328170040ffff4079a09bdbd0ca97592738773ee4f24fb67a7e5dea849aa31cfef189ec33dfd093d7fcf41416ed6d3f4febf12ac9a931fe33ce09c9e07aedff00f5d566d408ddf31edf4f6c1ec73ffebab5b356724425b159f512bd588e41fbd8cfd3d6abb6a58cfcddcf718efd7f3f43ee7bd696d1277febb956b37d2dfd77213a99ecc7a7af4edd07d2a26d489ee4e01ea7a93f8f278a124af62f78beb7febe446750c918739e474ede8bcf3de98da80e9b8e4f1c8ea3d303e83d714ccef6ba18750cff00167b74c63ea48ebc0cff002a84df139cb11d3a1e83a719e878e28f90d47677b7e1fe644d7f8c1393c8ce3bfaf6e48c66a06bfe7a93d7bf27b73fe45054777aedfd75206bee49dc7a762077e873d39f61e9ef559af08cf2d81edd7b63d8faf6a4b4bdddede562afaaff0081fd7c880de1271b9be5e0e48efdfdff000cf6a89eedcf058f3ea3907dfdf8f514691b596fd8add3bc9dd109b96191924e4f6ebef9ed50b5c67825ba8efe9edb7af3eb4b996aecca92d129abf98c373db3d06083f788e9dba9ff001aeefc257bb275407e6122e14e39f7f61ed512575cd635a4d73592d169ff000e7d67e10d676ac5fbc3818e32391c7233c83c57bae9baba3a2fcc7271dbe98007afe3506937b2b1b526a2ac872ddb1d7a7b7538ff00eb573f3dda92df3679f5073efcf53e941999925d02080ddcf19c8fae07e39feb545a7e4f4233d028e4fb71d79ffebd007e591f8b3aa724020938e1d9801ed9233d0f6f6f7a60f8a9ab9fe263d07dee4e490060ff002a4a51ebfd7fc39cd24f99592b2233f12f5671f2cacbdb93d3d8eee871efcd20f1f6ad27deb961dba9e47a1cf5f7abe6bd9a4ade8438b7657febc870f196a32119b873d4101bf0e39fe9562dfc55745b9b8938f57e4838e7afcdfad48a4ddd7f74ebec7c472b601b890f41f78a9038e5bb67a71ffebae920d66460312b724ff11e3b73f3601eb4fb8692deebfafc0ba35576fe36ea4753f91f98e0fa74fc69ff00da1d72cdcf3c927207241f9b9e9485a592b6b718da86ecf2475ee3e9c7a7d2a06d43a12dc638e7d3b9c1e4fbe681c95bd5959f50e9cb1ce7bf6f539ce3a7ff005ea06d4003c3027ea383ea3191d3fcfada8b77d09dada1036a1d72d9ebce7f538e98aacd7fd72c7b6327a7b75ebf8d546f6764bfaf987abdcaed7e392589ebdf9ebd067a722ab3dff5393d49ce47e201fc38ebd29dedf13b5cb96a9595fcc84df31c727bf527d0f5e78a69bb233cb8c60f00fe47239aa04bdd7795d790a6ea42a0ec97fef9e9efd0fa53bce9dbfe594bd307e5718f4cf14b99772b97449c6dfd77ff00871035c11c4720e0ff0001ebedcf418fa53c4778d91f679b391d8e0fb9f5c63f0a9735d06a1ccb5e9fd6e2fd975072545b4a7a7f0e3f019eb8e3bd28d33546200b693a818c74cf707f2f5a4e7d8152bd9cbdd251a0eb0d802d9ba81d4e0f538e9c7e7c54a9e17d6dfa427d3bf1f5c8e48f6fe54b9f7d3434f67b2be88b09e0cd6e4230871eea7923a83839078e95613c01ac3672a7391d11b1f539279e7f0a94dad996a295f4ba2dafc38d55b82ec1b91811b71eb8cf4ab29f0bb5163cbc9ce7f87f50318ff3d68bb5d587226ddd5cd183e14dd120b098e31ce3803d0f1d78ae834ef878da7ceafb64dc31cb6e238c71f951abeeec38a51bd95be47a369f04ba78451b805f63c63d3d3a576363e2592df0189ea38cfebfa522af7e86faf8b7e5e5c67d9b6e7b71f3704f1c7eb4dff849924382e3b77191ed8c7d78a044bfdb28d8f9ce72c7a8f61c7381cd2ff69a1cfcd8ebd4fd3a7271f9d007e37ac8c33c8fae3f519c64d3fcd2bd7380463d0f6e9d8f5edf8d739c7cb6697f5f88f598e7ae339e879ed9073d381d7faf352adc380464e4e7a03f98cf39fa55f3592496dfd7de5c135769e97d8985cb707736e07d3a8f435a105db6e1c9381fc27a0e7a8ea3fcf356a5a5dab12e2d6ef7feba9d2586a1246ca09e73c363001e80fddeb8fcebadb6d54b1cb3"),
                    Utility.HexStringToBytes("801a9e86000011987b368079000037f0406328170040fffff180482dd7a72bb8753fe4d514a374efba7f7fddfe67ffd5fe549752dc00de4a8c0d993d78e9f2f5ab2ba8704061d4f190013d3818c0ff00eb57a4a2ddedd0cdc5733f7843a9e323783927827818c0cfdde473d3b542da9e7037f739191cf51ce073deab95a7a7bbf313765cadbfebf12ab6a7d4ee27a0c63381e99c8cfd3b559b5796e9c6dddb7200e48cf38c96c7cb576ddb5fa825b257f33b2d3fc3ef71cb02c0e472b9079c71f2fcc7fcf6aeaa0f03ac98261cf03f1c74c823d2a6e9de293348c796fa9757e1ea360984f1d4a81c7bf0bc76fcaa64f87898e6dcf6e8b9ce339278ff003eb445493d515db42d2fc39438ff004739edf21071e98c1c0e3f955d8fe1c2631e47a8c6d23231d08239a2d6fb2bfaf2ea2d8bf1fc398c63f70a08238c7a76000e38357a2f8771823f70bd4754e9dbd38ce28e4b8ef7566f4469c5f0e11b69fb3a8e47400edcf38e471ef5a51fc378f8ff0047cf4e42607a63eef278ebfaf7a5ecfcff00afebcc7cdbfe86943f0d14e36db01c1e768e7b678071db8ebfceb522f8668307eceb8e074e38eb901783803bd57247b0b99a4d5f43461f86f0e47fa3a9c679c0fcfd9ab463f87912f5b70dd7b1dbc7d791df8f7a7ca9741a9bf534e0f87d09c0fb3ab119fe0da476c9e39f6ad18bc01073881720e0e54107af2001c9ce68b2edf80f99ef6d7fafeb72f47e0288e54c0bc67f83a0f45f4e7fcf7ad587e1fc442ab440b0c720003e9d3af18ff38a56d5ae5497a09c9b2e8f01c0a0edb75e0fde0a79c74fa74e7ad737a978261495774640de32547505802463a138f5ef435eec95ac272db999ec9e0ef801a4789208495462d8e1fa838f76c9fcabd2dff627b1bd8c3c08e19b3f77e561939f90ee5e300f56e31d6a54134b53d2a94a2a116b7b1c0f88bf61ad7a2577d2e7bc882676fcc640c7ae4ef56c01c7461f5af9fb5ffd997e2568523a88fed0b1b1c7fa3bae54772ca4f3fcbf314dc23ad9ec70f3dae9ad99e7d73f0e3c7fa76e5b9d2652509fb85b919ea03c639ac79f4cf1069f9fb5e9b7716d0013e5effcb6e491c7f9153c8fa6a5dfccfc8f120dc0e463ebd7a700f6fa726a4121c904ff0088f7e3a31fa66b976bea72cb469a7ff07f51de6e3209ce0e79047ffab8a3cdc639ee7b91903b91b79e7a7f9342f37f80a4a496aed6f97c89d25ea09249c76e3e9920e3afad684321c9c1cf4f7fcc0e9ffd7a7a27bfddfd68169e8ef7b7f5d8d38a42b820fae72381d7818ea7a715a31de32f1bbb85fc7d7ebfcab64efb3097359b4f434975320ae6423b67279f51c0e4f27fce6ae2ea2481f39001c7cc7048e9838e8473915716e3d3dd239a4eebb8e6d408c7cfc7d0e0e4ff00bdcfe27f0a81b50ce4f99f2e41fbc1bd803f5e29e92d2fb797f914e2f4bfa6fb0905cb4d3ac5b8e19c0c63f1c1e7dbff00af5edde0fd13ed1e5ee52c09cf03afbe29f3593d36dbfae845fded2fa3fe99f46e81e15dc885603b46dc90071dbf03efff00ebaf53d3bc26a026cb73e84b263f334a1d5dcd1cfb2d4e9a2f062b46095391bbaaf18f4e53f966ad45e0a00f08571d32ab93f500838c77feb9abbf912a4f6b97a1f0729e0c6092064e071e83d8f538e7ad5c5f0822e47967d17e4ebea402bd71dbd698733ef7255f07a2e0987ae70db40cf5f958e318e07d2ae43e1051d61f41c20c1ed90578ed47cc5cd2ee6b41e15518c46309818da327d873c1c75fceb76dfc1f113feafd01f9739e07038c1fd3eb42d3a8295aead7f534e3f0a2020794719f4fbc338c8cff001633c0cfd7d6eaf8610f0221800e55936e4e4723b9fd7af6a362478f0ba0c284500e0f43838ebfc391fd3d2a7ff84697682635cf38014fd300b0c13c8e28ee545d9ebb12c7e1d8d7384195c2e081c76c0ec3a74ebeb578682801c2ff007472a3f103191d3fcf4a872b68d58bba4bdddbfaee4b1e869fdc51d3391c83923e5c8e9c76e3e95a89a2461176c64ae0023ae769ed951b7a9ebf98a4e5d53fc07be97ba1"),
                    Utility.HexStringToBytes("801a9e87000011987b36807900003d88406328170040ffffeda4aaa1f914ae5b8214118f424f4f6fe75c56b7a528e4a01861f7976e7d31c0cf4f438c545deba932d2caf7b1ecbf0d6e92d3ca5dcab82ab8c0e7dc91d08e6bedcf07ebaab0c6a5c7cbb41ea39e41ce0e47be47e75b43e147ad29374636d5348f66b4bed3ae611e6c10b7017ee05cf1d47033deb95d6b40f0e5fef325a26e39ce234383ef9fe5438adad6389fc4d5b7f23c9f5af869e16bbdd8b683e70799225e3df2071c2f4f4af25d7be03f87b5047f2ed6ddb2319545c8ea72b9079c023159f2ca3b31f22d743f8c613b12467f51f4e703ae69e267e403f4c6323dfdb915ca70c649ecb543bce7cf5e3dc0c1ec7b71d39a5f35f232c7db8e9ee09ce28f2b17777bd9ff005ea598d89240cf5eb9edd813d8e7fcf7ad680be0104e33ea7afe7cfeb549b4ad7d8b8c934eeadf334a3278e0f7c9cf279e09f7e3356a31d08196e08e7a7bf5e4f1ff00ebab86a9ddf50d236b22eac40e0e7f103e63838ee3d8f7ff00ebdd54f53d8678e838eb8e9c7bd5f632bd9a6d8be5e339273c8e9f8f1b8f1da831e73fdd1918cf700f1fe7ff00af43f245bb36ecf5f5b7e66c6836fe76a3046c07ccdd36e30011d7d0ee3d6bed3f016801a28401d36e38e833ce720e3f90fc29bf852f525df46debe96febef3eb1f0f78715238f08371dbcb2e36f04e40efdb9e31ef5e9d63a18f942a90c4027e5c903a02c0af1ebdbda88e8d19ce56eba9d65b6848d85f2c311c15db9c63bb7ca48e3b77f5abe3418d5588886413c05eb8ed82bc638382462b6f208caf6bdc823d254b1f93637385c60367be18601dd9fcfd6ac47a42e3263380e33f27e381bb233f9d174ba8396974ae4dfd9409004640007f0939eb803819e9532e92a3691174e8413907a0c9ec7db3fe34aebb8295fa688bf1e92030daa4f4ff967f2e4e300e0671c7ad6ac7a72a29ca8c81c7cb8527ae0e472739efc7bd2ba49db7f3ff820e49752e4160bb43824fcac371000071d795e4f078cd489691ed2cc3736ee318e467a8f97fc7a1a86dc93d3444f3767f78cfb322eecf6383c281e992493c638efef9a6adba12782085c619476f5c678f4c7ad4ddaeb62f9862ac44ed2a0118e4a1fbbc7a8f6a914c7b54e4155dd8254763d18b75edf974a3bea3204b8863e09524310091d707079e73ce727b66ad7db20dbc3e4b67b71c0fbcbdc1c67b75f5a2d7be9b0d3b5ec2c773015619e724838209e9cf3d4e077ae2f5e962e71923777ea48ee727af4ff00eb520f2bdd2363c29a8f9522052c30c3047619e41f4e6bea0f0b6b0e5211b8e7e5009078ce338c74e739e3f0ad6324a28f5d37ec62ba9eeda46a92b463049e339c74f7391c1c76a9eeb537009cb9193d48da3839230791cd545f35f43924dc5dd239cb9d51c64673cf4239007607b73543fb589ea4f5f6e33db939fc3da996ba687f0cfe981ffd73e871f4fd69c3231d3bf71fae6bcf3cfdeda92283c0cf70dd074f6f5eddb8a9546e207eb8edebd79fce99a6ba2dd7e3f22fc11e7af4e38007af7e7aff009f6ad881718c67d390bdfb823bff009f7a766ae8695d5daebe5a9a91479c020f18fc71df83c37a7a7d6b4a28738e4f600e4648f5f41df8ada2b45643ba8a49a69fa9a91da30c1c3020f65193fc20b0ce7bfd3a75eb5756d98630a720f39c63e87d0fb76a6b5d1bb19b56d1adc4fb33280082bdbeefb7decfae6a1301cedc1e07a75c1edfe78a1ab5d7629d371d2daede9f71bfe17848d5a05c36438f46dbea4faf6e33fe35fa11f0d6c91e28189cb0087183907df0415e707209e9d2a9a6a0b4ea66f75ab3ec0d034d530c655540cec2493bba0f997033dbaf5fa835e97a6690bb826de0003214641f43cf078eb8fd6882d518d496f75748ffd6f9e2cf4948f395dafca8078ec0e09ee7800e383bbad589b4f884636954601b80a38fe2c0c37ca7771d3f035ec7c8f3a3aa5a9946cd0fdd50719ce146e3ee0bf4fc8e7d2a692d6358c6d382ac319c1c3e30490073d78f4f5a8b735ee9a486df2dc8a6"),
                    Utility.HexStringToBytes("809a9e88000011987b36807900004320406328170040ffff820d8a3ccc1c019dcb96ee413ebd703bfad5691a0418538c11d08665e99ce57d877e39fa555ad6d76136da6d2febf101736ca41dff0036006f94727d14af0063d7ff00ad5623beb7c4819c1f97008e40e800caf53c75edd3159ca2d75b8a2dcb4b0b0df441766e6dd82148239ea31cf538c7b0e951bea308c64a9dac3233e8718c678e073cf7a694748db998da96977cafd3fcccd9756505d14c7ebd181ebc607a7a7d2b3a6d6b6bb2971d0f0064b3fb024fe78a4d72cb6d0d231bdee66beb2cb920e0e31c1e4741c818cf7c73f8f534c7d59fcb3b5cf461b49c67d41e7927ebed49f2f466a95b43065d6260c32e37e71d7b73c7bf24e3fad68da6a4cf18049666c0e58707d720703fc69276d98e2af64ff0372da525480c7732e49238033d3e65e4ff002f5ac1d65778505d88de0745e39ea71d0e738038f7a454e2935a1bde19848923c70370c90076e0608f7ebd7eb5f4a78563e21ce78c7ae7b7523a03fd3d39a7e47a4972d38ddf43e80d2202d18dc060000647240ee373706acdcc05b8cfaae48fd4fcbedeff00d6b68ae55639a4f55e473f7368d9f949e7b01839c75ce7278fa7bd575b139e578c0e8791ea4f3d7ad5c62e438b5aa4b43f")
                };

            //Allocate the frame to hold the packets
            using (Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame restartFrame = new Media.Rtsp.Server.Streams.RFC2435Stream.RFC2435Frame())
            {
                //Build a RtpFrame from the jpegPackets
                foreach (byte[] binary in jpegPackets)
                {
                    //Create a temporary packet
                    Media.Rtp.RtpPacket interpreted = new Media.Rtp.RtpPacket(binary, 0);
                    restartFrame.Add(interpreted);
                }

                //Draw the frame
                using (System.Drawing.Image jpeg = restartFrame) jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                //Bytes of video should match byte for byte result.jpeg in the first scan exactly (From 0x26f -> EOI)

                System.IO.File.Delete("result.jpg");
            }

            
            
        }

        static void WinRtspInspector()
        {
            var f = new Tests.WinRtspInspector();

            Application.Run(f);
        }
    }
}
