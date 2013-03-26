using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {            
            RunTest(TestJpegFrame);
            RunTest(TestRtspMessage);
            RunTest(TestRtpPacket);
            RunTest(TestRtcpPacket);
            RunTest(TestRtpDump);
            RunTest(TestSdp);
            RunTest(TestRtpClient, 77);
            RunTest(RtspClientTests);
            RunTest(TestServer);
        }

        public static void RtspClientTests()
        {
            TestRtspClient("rtsp://178.218.212.102:1935/live/Stream1");
            TestRtspClient("rtsp://178.218.212.102:1935/live/Stream1", null, Rtsp.RtspClient.ClientProtocolType.Tcp);
            
            TestRtspClient("rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov");
            TestRtspClient("rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov", null, Rtsp.RtspClient.ClientProtocolType.Tcp);
            
            TestRtspClient("rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov");
            TestRtspClient("rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov", null, Rtsp.RtspClient.ClientProtocolType.Tcp);

            TestRtspClient("rtsp://195.191.142.77/axis-media/media.amp?videocodec=h264&streamprofile=Bandwidth", new System.Net.NetworkCredential("jay", "access"));
            TestRtspClient("rtsp://195.191.142.77/axis-media/media.amp?videocodec=h264&streamprofile=Bandwidth", new System.Net.NetworkCredential("jay", "access"), Rtsp.RtspClient.ClientProtocolType.Tcp);
            
            TestRtspClient("rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp");
        }

        /// <summary>
        /// Tests the RtpClient.
        /// </summary>
        private static void TestRtpClient()
        {
            //Get the local interface address
            System.Net.IPAddress localIp = Utility.GetFirstV4IPAddress();

            Rtp.RtpClient sender = Rtp.RtpClient.Sender(localIp);

            //Create a Session Description
            Sdp.SessionDescription SessionDescription = new Sdp.SessionDescription(1);

            //Add a MediaDescription to our Sdp on any port 17777 for RTP/AVP Transport using the RtpJpegPayloadType
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 17777, Rtsp.Server.Streams.RtpSource.RtpMediaProtocol, Rtp.JpegFrame.RtpJpegPayloadType));

            sender.RtcpPacketSent += (s, p) => Console.WriteLine("Sender Sent A RtcpPacket =>" + p.PacketType);
            sender.RtcpPacketReceieved += (s, p) => Console.WriteLine("Sender Received A RtcpPacket =>" + p.PacketType);
            sender.RtpPacketSent += (s, p) => Console.WriteLine("Sender Sent A RtpPacket =>" + p.SequenceNumber);

            Rtp.RtpClient receiver = Rtp.RtpClient.Receiever(Utility.GetFirstV4IPAddress());

            receiver.RtcpPacketSent += (s, p) => Console.WriteLine("Receiver Sent A RtcpPacket =>" + p.PacketType);
            receiver.RtcpPacketReceieved += (s, p) => Console.WriteLine("Receiver Received A RtcpPacket =>" + p.PacketType);
            receiver.RtpPacketReceieved += (s, p) => Console.WriteLine("Receiver Received A RtpPacket =>" + p.SequenceNumber);

            //Create and Add the required TransportContext's

            Rtp.RtpClient.TransportContext sendersContext = new Rtp.RtpClient.TransportContext(0, 1, (uint)DateTime.UtcNow.Ticks, SessionDescription.MediaDescriptions[0]),
                receiversContext = new Rtp.RtpClient.TransportContext(0, 1, sendersContext.LocalSynchronizationSourceIdentifier + 1, SessionDescription.MediaDescriptions[0]);

            Console.WriteLine("Senders SSRC = " + sendersContext.LocalSynchronizationSourceIdentifier);

            Console.WriteLine("Recievers SSRC = " + receiversContext.LocalSynchronizationSourceIdentifier);

            //Find open ports, 1 for Rtp, 1 for Rtcp
            int rtpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17777, false), rtcpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17778);

            int xrtpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10777, false), xrtcpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 10778);

            receiversContext.InitializeSockets(localIp, localIp, rtpPort, rtcpPort, xrtpPort, xrtcpPort);

            receiver.AddTransportContext(receiversContext);
            
            sendersContext.InitializeSockets(localIp, localIp, xrtpPort, xrtcpPort, rtpPort, rtcpPort);

            sender.AddTransportContext(sendersContext);

            //Connect the sender
            sender.Connect();

            //Connect the reciver
            receiver.Connect();

            //Send an initial report
            sender.SendSendersReports();
            
            //Above call triggers this when recieved
            //receiver.SendReceiversReports();

            Console.WriteLine("Connection Established,  Encoding Frame");

            //Make a frame
            Rtp.JpegFrame testFrame = new Rtp.JpegFrame(System.Drawing.Image.FromFile("video.jpg"), 100, sendersContext.LocalSynchronizationSourceIdentifier, 1);

            Console.WriteLine("Sending Encoded Frame");

            //Send it
            sender.SendRtpFrame(testFrame);
            Console.WriteLine("Sent, Sending Reports and Goodbye");

            //Send another report
            sender.SendSendersReports();

            //Measure QoE / QoS based on sent / received ratio.
            Console.WriteLine("Since : " + sendersContext.SendersReport.Sent);
            Console.WriteLine("-----------------------");
            Console.WriteLine("Sender Sent : " + sendersContext.SendersReport.SendersPacketCount + " Packets");

            System.Threading.Thread.Yield();

            if (receiversContext.RecieversReport != null)
            {
                //Determine what is actually being received by obtaining the TransportContext of the receiver            
                //In a real world program you would not have access to the receiversContext so you would look at the sendersContext.RecieverReport
                Console.WriteLine("Since : " + receiversContext.RecieversReport.Created);
                Console.WriteLine("-----------------------");
                if (receiversContext.RecieversReport.BlockCount > 0)
                {
                    Console.WriteLine("Receiver Lost : " + receiversContext.RecieversReport[0].CumulativePacketsLost + " Packets");
                }
            }

            System.Threading.Thread.Yield();

            //Or by comparing packets received
            if (receiver.TotalRtpPacketsReceieved < sender.TotalRtpPacketsSent) throw new Exception("Did not receive all packets");

            Console.WriteLine("Test Passed");

            //Send a goodbye
            //sender.SendGoodbyes();

            sender.Disconnect();

            receiver.Disconnect();
        }

        static void writeError(Exception ex)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("Test Failed!");
            Console.WriteLine("Exception.Message: " + ex.Message);
            Console.WriteLine("Press (A) to try again or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void writeSuccess(bool auto = true, string message = null)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.WriteLine("Test Passed!");
            if (!auto) Console.WriteLine("Press (W) to run again, (D) to debug or any other key to continue.");
            if (!string.IsNullOrWhiteSpace(message)) Console.WriteLine(message);
            Console.BackgroundColor = ConsoleColor.Black;
        }

        private static void RunTest(Action test, int count = 0)
        {
            System.Console.Clear();
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine("About to run test: " + test.Method.Name);
            Console.WriteLine("Press Q to skip or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;
            if (Console.ReadKey().Key == ConsoleKey.Q) return;
            else
            {
                Dictionary<int, Exception> log = null;

                int run = count, failures = 0, successes = 0; bool multipleTests = count > 0;

                if (multipleTests) log = new Dictionary<int, Exception>();

            Test:
                try
                {
                    System.Threading.Interlocked.Decrement(ref run);
                    test();
                    writeSuccess(multipleTests);
                    System.Threading.Interlocked.Increment(ref successes);
                }
                catch (Exception ex)
                {
                    System.Threading.Interlocked.Increment(ref failures);
                    writeError(ex);
                    if (multipleTests)
                    {
                        log.Add(run, ex);
                        System.Threading.Thread.Yield();
                    }
                }
                
                if (run >= 0) goto Test;
                else if (multipleTests)
                {
                    if (failures > successes) writeError(new Exception("More Failures then Successes"));
                    else writeSuccess(false, failures + " Failures, " + successes + " Successes");
                }

                ConsoleKey input = Console.ReadKey().Key;

                if (input == ConsoleKey.W) goto Test;
                else if (input == ConsoleKey.D) System.Diagnostics.Debugger.Break();
            }
        }

        private static void TestRtcpPacket()
        {
            Console.WriteLine("RtcpTest");
            Console.WriteLine("--------");
            byte[] example = new byte[] { 0x80, 0xc8, 0x00, 0x06, 0x43, 0x4a, 0x5f, 0x93, 0xd4, 0x92, 0xce, 0xd4, 0x2c, 0x49, 0xba, 0x5e, 0xc4, 0xd0, 0x9f, 0xf4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Rtcp.RtcpPacket asPacket = new Rtcp.RtcpPacket(example);
            Rtcp.SendersReport sr = new Rtcp.SendersReport(asPacket);
            Console.WriteLine(sr.SendersSynchronizationSourceIdentifier);//1928947603
            Console.WriteLine(sr.NtpTimestamp);//MSW = d4 92 ce d4, LSW = 2c 49 ba 5e
            sr.NtpTimestamp = sr.NtpTimestamp;//Ensure setting the value through a setter is correct
            if (sr.RtpTimestamp != 3302006772) throw new Exception("RtpTimestamp Invalid!");

            //Verify SendersReport byte for byte
            var output = sr.ToBytes();//should be exactly equal to example
            for (int i = 0; i < output.Length; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            //Recievers Report and Source Description
            example = new byte[] { 0x81,0xc9,0x00,0x07,0x69,0xf2,0x79,0x50,0x61,0x37,0x94,0x50,0xff,0xff,0xff,0xff,
                                0x00,0x01,0x00,0x52,0x00,0x00,0x0e,0xbb,0xce,0xd4,0xc8,0xf5,0x00,0x00,0x84,0x28,
                                0x81,0xca,0x00,0x04,0x69,0xf2,0x79,0x50,0x01,0x06,0x4a,0x61,0x79,0x2d,0x50,0x43,
                                0x00,0x00,0x00,0x00
            };

            //Could check for multiple packets with a function without having to keep track of the offset with the RtcpPacket.GetPackets Function
            Rtcp.RtcpPacket[] foundPackets = Rtcp.RtcpPacket.GetPackets(example);
            Console.WriteLine(foundPackets.Length);

            //Or manually for some reason
            asPacket = new Rtcp.RtcpPacket(example); // same as foundPackets[0]
            Rtcp.ReceiversReport rr = new Rtcp.ReceiversReport(asPacket);
            Console.WriteLine(rr.SendersSynchronizationSourceIdentifier);//1777498448
            Console.WriteLine(rr.BlockCount);//1
            Console.WriteLine(rr[0].SynchronizationSourceIdentifier);//1631032400
            Console.WriteLine(rr[0].FractionLost);//255/256 0xff
            Console.WriteLine(rr[0].CumulativePacketsLost);//-1, 0xff,0xff,0xff
            Console.WriteLine(rr[0].ExtendedHigestSequenceNumber);//65618, 00, 01, 00, 52
            Console.WriteLine(rr[0].InterArrivalJitter);//3771
            Console.WriteLine(rr[0].LastSendersReport);//3470051573

            //next packet offset by Length + RtcpHeader
            asPacket = new Rtcp.RtcpPacket(example, asPacket.Length + Rtcp.RtcpPacket.RtcpHeaderLength); //same as foundPackets[1] or asPacket.PacketLength
            Rtcp.SourceDescription sd = new Rtcp.SourceDescription(asPacket); //1 Chunk, CName

            //Verify RecieversReport byte for byte
            output = rr.ToBytes();//should be exactly equal to example
            for (int i = asPacket.PacketLength; i >= 0; --i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            int offset = output.Length;// +Rtcp.RtcpPacket.RtcpHeaderLength;

            //Verify Source Description byte for byte
            output = sd.ToBytes();
            for (int i = 0; i < output.Length; i++, offset++)
            {
                if (example[offset] != output[i]) throw new Exception();
            }

            //ApplicationSpecific - qtsi

            example = new byte[] { 0x81, 0xcc, 0x00, 0x06, 0x4e, 0xc8, 0x79, 0x50, 0x71, 0x74, 0x73, 0x69, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x61, 0x74, 0x00, 0x04, 0x00, 0x00, 0x00, 0x14 };

            asPacket = new Rtcp.RtcpPacket(example, 0);

            Rtcp.ApplicationSpecific app = new Rtcp.ApplicationSpecific(asPacket);

            if (app.Name != "qtsi") throw new Exception("Invalid App Packet Type");

            //Test making a packet with a known length in bytes

            sd = new Rtcp.SourceDescription();
            sd.Add(new Rtcp.SourceDescription.SourceDescriptionChunk(0x1AB7C080, new Rtcp.SourceDescription.SourceDescriptionItem(Rtcp.SourceDescription.SourceDescriptionType.CName, "FLABIA-PC"))); // SSRC(4) ItemType(1), Length(1), ItemValue(9), End(1) =  16 Bytes                        
            asPacket = sd; // Header = 4 Bytes

            if (asPacket.Length != 16 || asPacket.ToBytes()[3] != 4) throw new Exception("Invalid Length");
        }

        private static void TestRtpDump()
        {

            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("RtpDump Test - " + currentPath);

            #region Test Writer

            Rtp.RtpDump.DumpWriter writerBinary = new Rtp.RtpDump.DumpWriter(currentPath + @"\BinaryDump.rtpdump", Rtp.RtpDump.DumpFormat.Binary, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7), null, false);

            Rtp.RtpDump.DumpWriter writerAscii = new Rtp.RtpDump.DumpWriter(currentPath + @"\AsciiDump.rtpdump", Rtp.RtpDump.DumpFormat.Ascii, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7), null, false);

            Rtp.RtpDump.DumpWriter writerHex = new Rtp.RtpDump.DumpWriter(currentPath + @"\HexDump.rtpdump", Rtp.RtpDump.DumpFormat.Hex, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7), null, false);

            //Senders Report
            byte[] example = new byte[] { 0x80, 0xc8, 0x00, 0x06, 0x43, 0x4a, 0x5f, 0x93, 0xd4, 0x92, 0xce, 0xd4, 0x2c, 0x49, 0xba, 0x5e, 0xc4, 0xd0, 0x9f, 0xf4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            Rtcp.RtcpPacket packet = new Rtcp.RtcpPacket(example);

            //Write the packet to the dumps
            writerBinary.WritePacket(packet);
            writerAscii.WritePacket(packet);
            writerHex.WritePacket(packet);

            //Recievers Report and Source Description
            example = new byte[] { 0x81,0xc9,0x00,0x07,0x69,0xf2,0x79,0x50,0x61,0x37,0x94,0x50,0xff,0xff,0xff,0xff,
                                0x00,0x01,0x00,0x52,0x00,0x00,0x0e,0xbb,0xce,0xd4,0xc8,0xf5,0x00,0x00,0x84,0x28,
                                0x81,0xca,0x00,0x04,0x69,0xf2,0x79,0x50,0x01,0x06,0x4a,0x61,0x79,0x2d,0x50,0x43,
                                0x00,0x00,0x00,0x00
            };

            //Write the packets to the dumps
            foreach (Rtcp.RtcpPacket apacket in Rtcp.RtcpPacket.GetPackets(example))
            {
                writerBinary.WritePacket(apacket);
                writerAscii.WritePacket(apacket);
                writerHex.WritePacket(apacket);
            }

            writerAscii.Dispose();
            writerBinary.Dispose();
            writerHex.Dispose();

            #endregion

            #region Test Reader

            //Test the readers

            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\BinaryDump.rtpdump"))
            {

                Console.WriteLine("Successfully opened BinaryDump.rtpdump");

                Console.WriteLine("StartUtc: " + reader.StartTime);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SendersReport) throw new Exception();

                Console.WriteLine("Format: " + reader.Format);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.ReceiversReport) throw new Exception();

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception();

            }

            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\AsciiDump.rtpdump"))
            {
                Console.WriteLine("Successfully opened AsciiDump.rtpdump");

                Console.WriteLine("StartUtc: " + reader.StartTime);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SendersReport) throw new Exception();

                Console.WriteLine("Format: " + reader.Format);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.ReceiversReport) throw new Exception();

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception();

            }
            
            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\HexDump.rtpdump"))
            {

                Console.WriteLine("Successfully opened HexDump.rtpdump");

                Console.WriteLine("StartUtc: " + reader.StartTime);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SendersReport) throw new Exception();
                
                Console.WriteLine("Format: " + reader.Format);

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.ReceiversReport) throw new Exception();

                if (new Rtcp.RtcpPacket(reader.ReadNext()).PacketType != Rtcp.RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception();

            }

            #endregion

            #region Demonstrate DumpFormat.Header

            //Using the DumpFormat.Header will only allow RtpPackets, RtcpPackets will be silently ignored.
            //Also the Payload is not written only the RtpHeader
            using (Rtp.RtpDump.DumpWriter writerHeader = new Rtp.RtpDump.DumpWriter(currentPath + @"\HeaderDump.rtpdump", Rtp.RtpDump.DumpFormat.Header, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7), null, false))
            {
                writerHeader.WritePacket(new Rtp.RtpPacket(7)
                {
                    Marker = true,
                    SequenceNumber = 0x7777
                });
            }

            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\HeaderDump.rtpdump", Rtp.RtpDump.DumpFormat.Header))
            {

                Console.WriteLine("Successfully opened HeaderDump.rtpdump");

                Console.WriteLine("StartUtc: " + reader.StartTime);

                Rtp.RtpPacket headerPacket = new Rtp.RtpPacket(reader.ReadNext());

                if (headerPacket.Marker != true || headerPacket.SequenceNumber != 0x7777) throw new Exception();

                Console.WriteLine("Format: " + reader.Format);
            }

            #endregion

            #region Read Example rtpdump file 'bark.rtp' and Write the same file as 'mybark.rtp'

            //Maintain a count of how many packets were written for next test
            int writeCount;

            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\bark.rtp"))
            {
                //Write a file with the same attributes as the example file
                using (Rtp.RtpDump.DumpWriter writer = new Rtp.RtpDump.DumpWriter(currentPath + @"\mybark.rtp", reader.Format, reader.SourceAddress, reader.StartTime, false))
                {

                    Console.WriteLine("Successfully opened bark.rtp");

                    Console.WriteLine("StartUtc: " + reader.StartTime);

                    //Each item will be returned as a byte[] reguardless of format
                    byte[] itemBytes;

                    //Read all Rtp Packets from the file
                    while ((itemBytes = reader.ReadNext()) != null)
                    {

                        //Show the format for the first item (All others should have the same)
                        if (reader.ItemCount == 1)
                        {
                            Console.WriteLine("Format: " + reader.Format);
                        }

                        int version = itemBytes[0] >> 6;
                        //Some rtpdump files contain VAT Packets...
                        if (version != 2) continue;
                        byte payload = itemBytes[1];

                        if (payload >= (byte)Rtcp.RtcpPacket.RtcpPacketType.SendersReport && payload <= (byte)Rtcp.RtcpPacket.RtcpPacketType.ApplicationSpecific || payload >= 72 && payload <= 76)
                        {
                            //Could be compound packets
                            foreach (Rtcp.RtcpPacket rtcpPacket in Rtcp.RtcpPacket.GetPackets(itemBytes))
                            {
                                Console.WriteLine("Found Rtcp Packet: Type=" + rtcpPacket.PacketType + " , Length=" + rtcpPacket.Length);
                                writer.WritePacket(rtcpPacket);
                            }                            
                        }
                        else
                        {
                            Rtp.RtpPacket rtpPacket = new Rtp.RtpPacket(itemBytes);
                            Console.WriteLine("Found Rtp Packet: SequenceNum=" + rtpPacket.SequenceNumber + " , Timestamp=" + rtpPacket.TimeStamp + (rtpPacket.Marker ? " MARKER" : string.Empty));
                            writer.WritePacket(rtpPacket);
                        }
                    }

                    writeCount = writer.Count;

                }
            }

            //I would just do a ReadAllBytes and compare but....
            //The files are 8 bytes off in size, reason being in the example file has a compound RTCP packet (SR, SDES) 
            //When that packet is written as a compound and not 2 individual packets it saves a DumpItem overhead (8 bytes)

            #endregion

            #region Modify 'mybark.rtp' Add a single packet and verify integrity

            //Modify myBark to add single packet
            using (Rtp.RtpDump.DumpWriter writer = new Rtp.RtpDump.DumpWriter(currentPath + @"\mybark.rtp", Rtp.RtpDump.DumpFormat.Binary, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 7), null, false, true))
            {
                writer.WritePacket(new Rtp.RtpPacket(7)
                {
                    Marker = true,
                    SequenceNumber = 0x7777
                });
            }

            //Ensure modification worked
            using (Rtp.RtpDump.DumpReader reader = new Rtp.RtpDump.DumpReader(currentPath + @"\mybark.rtp"))
            {
                reader.ReadToEnd();
                reader.Skip(-1);
                Rtp.RtpPacket addedPacket = new Rtp.RtpPacket(reader.ReadNext());
                if (addedPacket.Marker != true || addedPacket.SequenceNumber != 0x7777) throw new Exception();
                if (reader.ItemCount != writeCount + 1) throw new Exception("Modify Test Failed");
                Console.WriteLine("Modified mybark.rtp, Added 1 Item Total Items: " + reader.ItemCount);
            }

            

            Console.WriteLine("Wrote a compatible file!");

            #endregion

            //Tests done.. delete files
            System.IO.File.Delete(currentPath + @"\mybark.rtp");

            System.IO.File.Delete(currentPath + @"\BinaryDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\AsciiDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\HexDump.rtpdump");

            System.IO.File.Delete(currentPath + @"\HeaderDump.rtpdump");
            
        }

        private static void TestRtpPacket()
        {
            Console.WriteLine("RtpTest");
            Console.WriteLine("--------");

            Rtp.RtpPacket p = new Rtp.RtpPacket();
            p.TimeStamp = 987654321;
            p.SequenceNumber = 7;
            p.Marker = true;
            p = new Rtp.RtpPacket(p.ToBytes());
            Console.WriteLine(p.TimeStamp);
            Console.WriteLine(p.SequenceNumber);

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
                                          0xBE, 0xE5, 0x39, 0x8D, // etc... 
                                      };
            Rtp.RtpPacket testPacket = new Rtp.RtpPacket(m_SamplePacketBytes);

            // Check extension values are what we expected.
            if (testPacket.ExtensionFlags != 0xABAC) throw new Exception("Expected Extension Flags Not Found");
            else Console.WriteLine("Found ExtensionFlags: " + testPacket.ExtensionFlags.ToString("X"));

            // The extension data length is 3 (DWORDS) but this property exposes the length of the ExtensionData in bytes
            if (testPacket.ExtensionLength != 12) throw new Exception("Expected ExtensionLength not found");
            else Console.WriteLine("Found ExtensionLength: " + testPacket.ExtensionLength);

            // Test the extension data is correct
            byte[] expected = { 0xD4, 0xBB, 0x8A, 0x43, 0xFE, 0x7A, 0xC8, 0x1E, 0x00, 0xD3, 0x00, 0x00 };
            if (!testPacket.ExtensionData.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
            else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(testPacket.ExtensionData));

        }

        static void TestRtspMessage()
        {
            Rtsp.RtspRequest request = new Rtsp.RtspRequest();
            request.CSeq = 2;
            request.Location = new Uri("rtsp://someServer.com");
            Rtsp.RtspResponse response = new Rtsp.RtspResponse();

            Rtsp.RtspMessage message = request;

            byte[] bytes = message.ToBytes();

            Rtsp.RtspRequest fromBytes = new Rtsp.RtspRequest(bytes);

            if (!(fromBytes.Location == request.Location && fromBytes.CSeq == request.CSeq))
            {
                throw new Exception("Location Or CSeq does not match!");
            }
        }

        static void TestRtspClient(string location, System.Net.NetworkCredential cred = null, Rtsp.RtspClient.ClientProtocolType? protocol = null)
        {

            Console.WriteLine("Location = \"" + location + "\" " + (protocol.HasValue ? "Using Rtp Protocol: " + protocol.Value : string.Empty) + " Press a key to continue. Press Q to Skip");
            Rtsp.RtspClient client = null;
            //Make a client
            //This host uses Udp but also supports Tcp if Nat fails
            //rtsp://195.252.113.40:554/rts - Weird Server with no supported methods
        StartTest:
            if (Console.ReadKey().Key != ConsoleKey.Q)
            {
                client = new Rtsp.RtspClient(location, protocol);

                if (cred != null) client.Credential = cred;

                //Assign some events (Could log each packet to a dump here)
                client.OnConnect += (sender, args) => { Console.WriteLine("Connected to :" + client.Location); };
                client.OnRequest += (sender, request) => { Console.WriteLine("Client Requested :" + request.Location + " " + request.Method); };
                client.OnResponse += (sender, request, response) => { Console.WriteLine("Client got response :" + response.StatusCode + ", for request: " + request.Location + " " + request.Method); };
                client.OnPlay += (sender, args) =>
                {
                    //Indicate if LivePlay
                    if (client.LivePlay)
                    {
                        Console.WriteLine("Playing from Live Source");
                    }

                    //Indicate if StartTime is found
                    if (client.StartTime.HasValue)
                    {
                        Console.WriteLine("Media Start Time:" + client.StartTime);

                    }

                    //Indicate if EndTime is found
                    if (client.EndTime.HasValue)
                    {
                        Console.WriteLine("Media End Time:" + client.EndTime);
                    }
                };
                client.OnDisconnect += (sender, args) => { Console.WriteLine("Disconnected from :" + client.Location); };

                try
                {
                    //Try to StartListening
                    client.StartListening();
                }
                catch (Exception ex)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("Was unable to StartListening: " + ex.Message);
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                //If We are connected
                if (client.Connected && client.Client != null)
                {

                    //Add some more events once Listening
                    client.Client.RtpPacketReceieved += (sender, rtpPacket) => { Console.WriteLine("Got a RTP packet, SequenceNo = " + rtpPacket.SequenceNumber + " Channel = " + rtpPacket.Channel + " PayloadType = " + rtpPacket.PayloadType + " Length = " + rtpPacket.Length); };
                    client.Client.RtpFrameChanged += (sender, rtpFrame) => { Console.BackgroundColor = ConsoleColor.Blue; Console.WriteLine("Got a RTPFrame PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.Complete); Console.BackgroundColor = ConsoleColor.Black; };
                    client.Client.RtcpPacketReceieved += (sender, rtcpPacket) => { Console.WriteLine("Got a RTCP packet Channel= " + rtcpPacket.Channel + " Type=" + rtcpPacket.PacketType + " Length=" + rtcpPacket.Length + " Bytes = " + BitConverter.ToString(rtcpPacket.Payload)); };
                    client.Client.RtcpPacketSent += (sender, rtcpPacket) => { Console.BackgroundColor = ConsoleColor.Green; Console.WriteLine("Sent a RTCP packet Channel= " + rtcpPacket.Channel + " Type=" + rtcpPacket.PacketType + " Length=" + rtcpPacket.Length + " Bytes = " + BitConverter.ToString(rtcpPacket.Payload)); Console.BackgroundColor = ConsoleColor.Black; };

                    Console.WriteLine("Waiting for packets... Press Q to exit");

                    //Ensure we recieve a bunch of packets before we say the test is good
                    while (Console.ReadKey().Key != ConsoleKey.Q) { }

                    try
                    {
                        //Send a few requests just because
                        var one = client.SendOptions();
                        var two = client.SendOptions();
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.WriteLine("Sending Options Success");
                        Console.WriteLine(string.Join(" ", one.GetHeaders()));
                        Console.WriteLine(string.Join(" ", two.GetHeaders()));
                        Console.BackgroundColor = ConsoleColor.Black;

                        //Print information before disconnecting
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.WriteLine("RtcpBytes Sent: " + client.Client.TotalRtcpBytesSent);
                        Console.WriteLine("Rtcp Packets Sent: " + client.Client.TotalRtcpPacketsSent);
                        Console.WriteLine("RtcpBytes Recieved: " + client.Client.TotalRtcpBytesReceieved);
                        Console.WriteLine("Rtcp Packets Recieved: " + client.Client.TotalRtcpPacketsReceieved);
                        Console.WriteLine("Rtp Packets Recieved: " + client.Client.TotalRtpPacketsReceieved);
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    catch
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("Sending Options Failed");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }

                    //All done with the client
                    client.StopListening();
                }
            }
        }

        static void TestSdp()
        {
            Sdp.SessionDescription sd = new Sdp.SessionDescription(@"v=0
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

            sd = new Sdp.SessionDescription(@"v=0
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

            Sdp.SessionDescriptionLine mpeg4IodLine = sd.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p=>p.Contains("mpeg4-iod"))).FirstOrDefault();

            Sdp.SessionDescriptionLine connectionLine = sd.Lines.Where(l => l.Type == 'c').FirstOrDefault();

            //make a new Sdp using the media descriptions from the old but a new name

            sd = new Sdp.SessionDescription(0)
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

            sd = new Sdp.SessionDescription("v=0\r\no=StreamingServer 3219006489 1223277283000 IN IP4 10.8.127.4\r\ns=/sample_100kbit.mp4\r\nu=http:///\r\ne=admin@\r\nc=IN IP4 0.0.0.0\r\nb=AS:96\r\nt=0 0\r\na=control:*\r\na=mpeg4-iod:\"data:application/mpeg4-iod;base64,AoJrAE///w/z/wOBdgABQNhkYXRhOmF\"");

            Console.WriteLine(sd);

        }

        /// <summary>
        /// Tests the RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        static void TestServer()
        {
            //Setup a RtspServer on port 554
            Rtsp.RtspServer server = new Rtsp.RtspServer();

            //The server will take in RtspSourceStreams and make them available locally

            //H263 Stream Tcp Exposed @ rtsp://localhost/live/Alpha through Udp and Tcp (Source is YouTube hosted video which explains how you can get a Rtsp Uri to any YouTube video)
            Rtsp.Server.Streams.RtspSourceStream source = new Rtsp.Server.Streams.RtspSourceStream("Alpha", "rtsp://v4.cache5.c.youtube.com/CjYLENy73wIaLQlg0fcbksoOZBMYDSANFEIJbXYtZ29vZ2xlSARSBXdhdGNoYNWajp7Cv7WoUQw=/0/0/0/video.3gp");
            
            //If the stream had a username and password
            //source.Client.Credential = new System.Net.NetworkCredential("user", "password");
            
            //Add the stream to the server
            server.AddStream(source);

            //MPEG4 Stream Tcp Exposed @ rtsp://localhost/live/Beta through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Beta", "rtsp://178.218.212.102:1935/live/Stream1"));
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("BetaTcp", "rtsp://178.218.212.102:1935/live/Stream1", Rtsp.RtspClient.ClientProtocolType.Tcp));

            ////H264 Stream -> Udp available but causes switch to TCP if NAT Fails - Exposed @ rtsp://localhost/live/Gamma through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Gamma", "rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov"));
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("GammaTcp", "rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov", Rtsp.RtspClient.ClientProtocolType.Tcp));

            ////H264 Stream -> Udp available but causes switch to TCP if NAT Fails - Exposed @ rtsp://localhost/live/Delta through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Delta", "rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov"));
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("DeltaTcp", "rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov", Rtsp.RtspClient.ClientProtocolType.Tcp));

            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Omega", "rtsp://195.191.142.77/axis-media/media.amp?videocodec=h264&streamprofile=Bandwidth", new System.Net.NetworkCredential("jay", "access")));
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("OmegaTcp", "rtsp://195.191.142.77/axis-media/media.amp?videocodec=h264&streamprofile=Bandwidth", new System.Net.NetworkCredential("jay", "access"), System.Net.AuthenticationSchemes.Basic, Rtsp.RtspClient.ClientProtocolType.Tcp));

            //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/Pics through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.JpegRtpImageSource("Pics", System.Reflection.Assembly.GetExecutingAssembly().Location) { Loop = true });

            //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/SamplePictures through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.JpegRtpImageSource("SamplePictures", @"C:\Users\Public\Pictures\Sample Pictures\") { Loop = true });

            //Start the server
            server.Start();

            //If you add more streams they will be started once the server is started

            Console.WriteLine("Listening on: " + server.LocalEndPoint);

            Console.WriteLine("Waiting for input................");
            Console.WriteLine("Press 'U' to Enable Udp on RtspServer");
            Console.WriteLine("Press 'H' to Enable Http on RtspServer");
            Console.WriteLine("Press 'T' to Perform Load SubTest on RtspServer");

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
        /// Tests the Rtp and RtspClient in various modes (Against the server)
        /// </summary>
        static void SubTestLoad(Rtsp.RtspServer server)
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
                    //Use Rtsp / Http
                    using (Rtsp.RtspClient httpClient = new Rtsp.RtspClient("http://localhost/live/Alpha"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Http / Rtsp Test");

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
                    //Use Rtsp / Udp
                    using (Rtsp.RtspClient udpClient = new Rtsp.RtspClient("rtspu://localhost/live/Alpha"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Udp / Rtsp Test");

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
                    //Use Rtsp / Tcp
                    using (Rtsp.RtspClient tcpClient = new Rtsp.RtspClient("rtsp://localhost/live/Alpha"))
                    {
                        try
                        {
                            Console.WriteLine("Performing Rtsp / Tcp Test");

                            tcpClient.StartListening();

                            while (tcpClient.Client.TotalRtpBytesReceieved <= 1024) { }

                            Console.BackgroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Test passed");
                            Console.BackgroundColor = ConsoleColor.Black;

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

        static void TestJpegFrame()
        {
            //Create a JpegFrame from a Image (Encoding performed)
            Rtp.JpegFrame f = new Rtp.JpegFrame(System.Drawing.Image.FromFile("video.jpg"));
            //Save the JpegFrame as a Image
            using (System.Drawing.Image jpeg = f)
            {
                jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            //Create a JpegFrame from an existing RtpFrame (No Encoding / Decoding Performed)
            Rtp.JpegFrame t = new Rtp.JpegFrame(f);
            using (System.Drawing.Image jpeg = t)
            {
                jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            //Create a JpegFrame from an existing RtpFrame by (Decoding Performed)                
            t = new Rtp.JpegFrame();
            foreach (Rtp.RtpPacket p in f)
            {
                t.Add(p);
            }

            //Save JpegFrame as Image
            //Todo find out why this fails... (System.Interop.ExternalException - Generic Error in GDI+ has occured.)
            using (System.Drawing.Image jpeg = t)
            {
                jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            byte[][] jpegPackets = new byte[][]{
                Utility.HexStringToBytes("801ac9e26c6b82a24dce0864000000004132381f0054ffffe5d6a414c5a916980e029d4829d400628c514b400507a514374a40463bd4a053179c7d6a414864b10e33daa602a28ba63f1a9450264d6eb9901f4a9a6fba05456df7cfd2a4b8246d18a04478e0d14849c1e307de8f98fa0fd6980ea514d19c727f2e29c00c7393f8d0038714fcfcabf53fd2a30a318ebf539a7055ce42afe5400ec80704807eb4a0823820fd29013da9f92437278534804073d9bf234bf81a3340a6028cf391d7dc52e4e3803f3a052e69000dddc2fe7ffd6a5e72064027f1ff003d68a33cafe3fd28017e6c755ffbe7ff00af4a037f787e5ffd7a334b400a79183c7d29029ebb8fe94a3a75a5eb40114884ff0019fd2a0492f6d8ba40c8413bbe64cfb7f4ab6464f14dda777e1fd4d00402f7551c6e84ff00db214efb76ad9e1e1e9de21530534bb4d0043f6fd5874687fefd0a8eeaff005492dda291e2d8e30711804fe3568a9c5412c6cd4f4032824bceec7e140593241038f7ad1f20fa5279077671d40a7a08cf226eca3f127fc29009fba2fe67fc2b47c86f4a3c83e940144799dc0fc29a5dc0cf96c7f2ad0f24fa527927d280288924ce3ca61f88a77da9a32003267fd9563fcaae793c8e29a22c81914015a3d69d64d897b2ab7a6f22ac9d62f3690d792952390cf907f3a0c4076a4f287a5164045f6b59410c903e7ae23507f31cd355adf3f35b447fe04c3fad4e60523a537ecd1ff7450529497521f2ad59b263907b2c800fd41ab766d696cce544c778031907f962a1fb32f3d7f334f8ad3272ae411ef43437524d59b3654ee5070467b375ac6d7acf7a19d3a8fbdfe356196ef18172c47e14d93ed6c9b1ca3afbafff005e85a1072b22ed07a5462674380d91ee2aeea16d240edb9786e47bd669041071d2958b4ec5e5a7ad3074a914532478a5a414b40052d14521852374a5a6b9e0d0009ce2a414c5eb4f1523278f951ea38a90531704023d29e2989966dbf8a966fbe3e945b8c2b1f7c536627cde3047d6810946690838ce47e549827ab1fc2801c7a7f9f5a75340ec4934a1476247e26801dd29770032481f8d376af7507f0a55c0381c71db8a0072ba1fe35fce9ea701860f2a7a0a6034ea0050dc7009fc28dc47f09fd3fc6814500286c8fba47d697e6c8c053f8fff005a85383f851400a198e0607e7ffd6a71dc71c8e33dbe94da5a003e7fef2ffdf27fc69c377f787e5ffd7a4140e2900edd8ea7f4a7727b9a631f947d453b3400b8ff0069bf4a503dcd3734b9a00500e3ef1fd29467fbc69b9a33c50029381924fe9487af7a6b1f90fe1fcc519cd301dc01de8279c8eb4dcd2d001923d0d197edb7f2a3345001cf723fef9ffebd2faff85349e07d7fa1a5a003273f747e74a07a85a334669dc0085f4cfe149853fc3fca9734669008557b8a4d8a4f43f952e73f9d2f14ee030c480f5c53d220071400339a7601ebd6810bb290ad2ed19e9f95371900f3f9d0052d52d4cf6c71f7979fad72b30dbb97bd76e41f5ac2d4f4cc192753f2804914d6a34670e9520a60a916801c28a296800a28a5a004a6b73c53e98c32690c72f534f14d4e87eb4f03240a4326518e076269e29a3900fad3c5325966dc1f2c9f7a6487329a9213fbb5a89c1f30e0f7f4a5d400d148727be31e9401fed1fd2980ea75315413d4fe74b8fae3dcd201d4ece08edc7f8d302803ee8fca9c303b0fca9806f51fc429db8638607e9464f1c9a72f240340081b3d8fe4680c3d0fe469173b17af4a7520004b03856e7a76a70247546fd3fc693f0a5a00371e9b1bf4a033ff0073f5a514a38073400996271b571fef7ff5a9d96e9b57fefaff00eb52528a0018bed230bf9fff005a90193d1697a52f7a004fde7aa0fce973274cafeb477a70e87fcf7a40372fdcafe5465b1d4528eb45301a7710471f97ff005e8cb67ffad4ec51400dcb7a0a5cbff747e74ea3191400d05b1d07e74b96f6fcff00fad4eeb49400d25b03a139ed9ff0a379cfdd34ec514009b89e369fcc51bbdbf514b46060"),
                Utility.HexStringToBytes("801ac9e36c6b82a24dce0864000005904132381f0054ffff50026f19e87f2a5cfb1fc8d181463da8010301c1207e346f5fef0fce9481e951cb3470ff00ac9029f4cf3f95004a187ad3b354fedb69de61ff007c9ff0a9167b76008962fc58034c45acd266abbc91201c924f6534c92e4229f2c166f73c5202cb100124e07a9acbd4b50b77b3963490ef3c0e383f8d57ba9ae66c873f2fa0e05526423a8229ad067fffd0e6c548b4c1520a621d4514b4c0051452d2012987ef0a90d33196a431c83826a4438653ef4d5fbb4e033fce901328c000f514f1483ad380aa116a21845a84f2e6ac27dc5c7a556e72791f95480a69074a0a9f523e94801fef1a603a9d4dda3dff003a5031d09fccd003852d2607700d1b5777dd5fca801c08f5a55650c0b30033eb48147a0fca9e09c75a0062b2850370e9eb4f0ca7f887e7467d697f1a003231d68c8f7fca9c3a3727ee9fe547e3400d0c3d0ffdf269d95c1eb9f4da6968a402647be7e86947e3f952e3de96801b9e78068ddec7f1c53c7193ec7f95371c50026f3fdd3f98ff001a50f9cfc87f31fe34b8a5a00404f5d87f31fe3464ff0071bf4ff1a5a3bd0210b3019f2cfe63fc69771e30a48c7b7f8d2ae7701ef4d8ff00d5afd2818b96ec8c7f2ff1a3271f71bf4a7514084ddfec9fca82ca3d7f234b9a5ebde801bb97b13ff7c9ff000a370ff6bfef934e1d40f7a6a93b475e94c00b0079047d41a37a91d452d19c77a40207539e40e7b9146f51fc4bf9d2e4d35dd510b390147249a00cfd5351f20f93037ce47ccc3f87ff00af58de61ce49249a8679ccb3bb9eacc4d393e6207a9c55dac3274cbb6d5193e956e3b60a3748727fba0d321dd0ae026d1ea475ab1b95d772f38ea3d286c405fd3a5391b70eb51a8c92a0f5e94c864d9361ba1a9026f95ced270e2a3911979ebef4eb888839fc8d352e0f46eb4c0a02a45a81240c323f1a90381401352679a8bcca50fcd171930a5a6a9a750021a60fbff853e9a07cf9a0648a3814f41938f634d5e83e94e4e1c7d6a409c528eb40a515422daf083e955c77ab2d800fb0aad8273cd20148e47d2905260ff78f1f4a500ff78fe9400e14a29b838ce7da9c063de80140a5fe2fc29001e94bb47a7ea6980a29c3d69a140e31fa9a70008c60700e334805a00a4dab8fb8bf952854fee2fe54c0763e56ff0074e3f2a5c7b534469c7eed78f6a5d887f817f2a005e9d6972319cf140007455fc8538e319c0e0e3a5201a31cf238a3207714bf97e54b9fa7e54009b970791c8346e1ea297d38fd2941fa7e5400dde83ab0fce93cc41fc43f3a7d2124e39ffebd0037cc4e9b87e746f43c6f5fce9f934162473400d8dd19c00c0f3d8d355d0281b8640ee7a54993fe4527e03f2a004f313fbebf9d2865ecc3f3a383d71f95040c1f9474f4a042e41c7239e9ef4b9c5376a631b17f2a5d898e113f2a431473401814df2a31c845fca8d898e514fe14c43a8a6f969fdd1f951b107f08a005c5676bae534d6c1c0660a7dfbff4ad0daa3b7ea6b3f5c883e9923027e421b193eb8feb42dc0e633cd6869d1237ef646c60e17dcd45a7d9fda5fcc7e224ebc753e95a6ca493b0231ff6b93f8537d860c7e5e0e47e62a03218dc3af1ea29cb3491365a31b3a103a53e4891d77c6721bb51610b90db644c61b8fa1a64ea7938c11cd560ed03151f74f6ab41c4a80f714016addc5c5be09f987bd56910027b54769298ae31d89c62afdc440fcca3834d7603934b96120feee79fa55f06b24301d56b4a0cf960b1249ee6a6dd06a362714a29b4a290c951aa4cd400e29e1a9dc093b5087934d07342fde3f4a00997ee8a72fde07d0d347000a7c7d4ff009ef40c9c538534528a64975feeb7d2ab0a9d8fca73e9558671d6900ea052156f5ebfe7d2902b7f7cfe9fe1401263e51f5ff1a514cc1c6371fd29429fef9fd2801e053853304f563f90a7153c7cc7a7a5301c29c3a1fa530039fbc6976f1f78f3400e5e94b4c0a3fbcdf9d3828f56fce801d4b4985c1c96e013d68c0ff6bf3a403a97"),
                Utility.HexStringToBytes("801ac9e46c6b82a24dce086400000b204132381f0054fffff83fe043f91a610b8eff009d280a3fbd8ff7a801d9a334d2abfed7fdf546d4ff006bf3a007514c2ab83f7b804fdea36af4cb7fdf5400ff006a0e083cd370bfed7fdf546140c7cdcf5f9a801d4714ddaa07f1ff00df549b54ff007ffefaa007d19a6054f57ffbea8c023abfe74807d04fcadf4a60007f13fe74b8c64091b07e94c07f18a01a660e7efb0fc07f851b7fe9a37e43fc29012525371e8edfa7f851b4e4e5db8fa7f85310fa4ec69a41cfde3fa7f8520c83f78d201c4d473a09609232400ca5727a0c8eb4ac4ff7bf4a8249776554823d453b814ed63482d520ce081cfb9ef4e28072a391cf14d650463a9a760140dc8f7140036d201c707f4a80a142c8bd3ef2fb1a9c125581fd2a22dcfb8aa02b4c04aa4e30c2a04768ce71c54ec76b93400b93e9d7148632470cc1d7f1ad5b7904d6c0e791c1acd923001c0c77a9f4f930ac87a75a13d44cffd1e018735aabd2b2dba8ad45e940d8f14b4828a403e9453452d00480d2a7de63ed4ccd393bd3113835245cb5462a48bef50b702714e5eb4c14f4fbc3eb4c92d9fbadf4aae3a5586fbadf4aaa338edf95201cddbe9452127da8e7da818e14e14d00fa81f85033ea3f2a0078a7139c7b0c5339f51f952807aeefaf14c43c52f14c00fa8fca972402723039e9400fa3eb4d00fafe94b83fde1f95031dd9bfdd3fca97a5306ef518208e9465b1fc39a403e8cd34163d428fc68f9bb053f8d003e9293e6edb7f5a4c9c76fce801dd430ff64ff2a3229a0b772a7f1347cc7a6dfce801d9a334d39cff000fe67fc28c1f55fccff85003b34669b83eabefc9ff000a5c1f55c7d4ff008500283c8a4e9c7bd261bfd9c7d4ff0085261b24928727fbc7fc2801d9a5cf34986ff67fefa3fe1498600e42f1ee7fc2900ecd28351e5b3fc3ff007d1ff0a0eecff0ff00df47fc2981266827e63f5f5a8f2c38c2fe7ffd6a425813f77f3a404b9a696a8f79f41f9d4534db46de869004f30cec04e3be2a14932c78c7a544ec7b8eb429c8aa403f8009c71dfda9c32808c6e5ef4d00920746ec734e39ce7eeb77aa4891a7e53b94f06a29796c8e2a46ef8fc4544fd29b1a2bc9d314c57c01ec69d272bef50ab135032c96ca1ef4c47d8fc1a01dd190bf7b1d2aab9753c8a10194dd6b517a565b75ad4031414f71d4a2929452014528a414b400ea7af7a60a7af7a6226ef5245f7aa3a922fbd4202714f8ff00d62fd453053e3e245fa8aa24b6c3e571ed55874ab2ff0075be955097ed8c7d2900ea29997cf6fc07ff005e9416effcbffaf40127f0e7de814c0cd8c1e99a505bd07e74012528a8f737f747e74ecb7a0fcfff00ad400f0697aab7fba7f953327d07e746e3b4fca7a1140126697351873fdc6fd29777fb27f4a007e68cd3777b1a4dc7fbadfa5031f9a527f9d30b74055bf2a378c1f95bafa5003f3453030feebfe546f07b1ffbe4d003a8a6eef66ffbe4d2e7d9bfef93400b9a3b1e33d3f9d349f66ffbe4d058608c1e7fd934807514ddc3d0fe546e1eb400e3499a6ef5f5a4f317fbc2810fcd19e0fb8351f98bfde14d32af76a00973485aa1332e3ef0fce9a6653d0d219317a6990d426418f5a8ddd88eb8149b1a43e5b9daa554fcdd33e95485cb070b2e39fe2ff1a7b1a608f77515372f97427dd81c83b73c8ee28385c11f75b91ef512830e327e4f53dbff00ad522edc9560769ea07f31ef5ac75337a0866f2d4b7240a48f5156f9641c763515cc676ed5391fceb3d8943822af624da322939073ed51b904706b285e14e0838a78be51f36e1523b16e4e149aa72c9b109ce297fb421c105bf0c550b99c4bc20217de95865986e9b821b9ab22549461bad62d488ee07cac7e9400d6ed5a8bcf3596ddab4a1ff56bf414ba14f724a5ef45148428a5a414b40c70a70ea7e94d1da9c3a9fa53113d4917dea8874152467069f502714f4fbcbf5a60a72fde1cd324bac72aff004aaa3a54cc7e56fa55604fd45201c4fa500d34b1c7ddcfe34819b3"),
                Utility.HexStringToBytes("801ac9e56c6b82a24dce0864000010b04132381f0054fffff77f5a00929d5182719c714e073d8d031d4bdcd373ed4e279ffeb502141a5a6823dff2a51d3a11f85301d45276ea3f3a3728ea40a431d9c0a5a6ee5c1f987d3346e5c7de1f9d003e8e948083d0e47ad008f5a042d1476a3f1a06049d8719cf1fce8cfbd1fc2df87f31494085dc71d4d1b8fad373de93340c76e3eb49be9a4d34d201c5cf3834cde71d4d2134d270326900e2e7b1a6b4840e4d44d30e8bc9a66d673f3526ca516c93ce24e14fe34e5567fbc49a548c0a99462a7565a8a433ca02a274c022a777da0e0556676249c50323f28934e58c8a91589ed4166a120b8c75f9483d0f159d1cdf6693c9909f2fa29f4f6abd2be0649a856249183c8623e818918fc856a8ce43649719eebe955e4d8e7fbdfceb44db41275b8419ff006bfc6952c628db2971183ea581ad2cccae604ef1a292b8354ddcb9c9fcab6b58b2fdcf9e2589994fcc170091d33c562e322a5ab14840334edb428a91690c848a54ce78a95d011c546a763f3d2803ffd2e04fdd15a507faa4ff00747f2ace6185fc6b42d8ee850e7b629805c4fe4a0200249c60d5717ef9e5171ed9a2fcf2833eb552a40bc35019ff005440ff007b3527dbe1cf01ff00103fc6b368a606bfdb2df1feb47e47fc2a78dd24c9460dc763586ab9fe203eb5a3a6295f34641e9c839f5a76d00d25fba3e94f4ebfe7d6a35e829ebf7852ea05814e069a296a892d1380df4a80743531ef55d4919e32290c75029b939e94a1b9fba6801ffc3f8d2d341f969c0e7b1a005a5a4e9d69411f4fad021714e1de99b973d47e74ecf0453187a5281834528a0051fa51f8500d1f4a004dabe83f2a36af1f2af3df14bd694f6fc7fa521081547f0afe548550ff0008a75140c618d08c6da6ac11a8c2a607a0a9290f1400d31a63a7eb48513fbbfa9a7122a369157bd201485f43f99a6909e873f5a699189e17f3a6ec24658fe149b4528b06741c0049fa9a80ab31e4f156360c7149b38a9bb2d4522248c0a9940f4a43c0a4078a56289460d2e714c069bbc034c43d8e2984f34d3275a89e6029d809d719a64a76f3daaac972171cf354afaf5f688d4904f24d52422d2cc5e4dc4371d306ac09037024247a15008fe79ac8b7bac48be601e9bb15a22fe204e0003b605691b33195c9c24cc542ca013d8afb5485275e1a407fe00b555efa238c4ae08fee81c5385fdbb280f2c84f73cd55913a84aaecacace30c307e51d2b9d9a330ccd193d0f5f5add92e6d9ba3c86b335058982bc4493d1b2293486afd8a629ea6983919a70a82897b544eb5229a185301b263cafd6acda902dd3d79cfe755ca3328152421a35c75a8191de106618ecbcd57a9e546672d8eb4d68994904107de9a8b15c8a9f1ecddf38247b1c50c9800e73ebed4f8770395e9f81f7fe95496ba85c7791bbee06c9e83afa7f88ab7a6a85f34039cedea31dbff00af54c8f9b9e08c01807d3ffad57ac881348a083919c83ee47f9f6c5392424cbebd053d3ef0a8d4f14f53cd665966969a2973544167a8f7a857bd48a781eb8a8416c9f978a9ea31d4b4c2cdfddcd01cff0074d302414e15187cf634e0c33dff002a0078fad2ff001114cde3b9fce94b0dfd410475cd003e82148c1507ea29052170a2801f85c63031f4a8257f98220efc9a492e02b0fee914d170992697a1a463d5976309b39a46319e15ab366be646564380a0e7dfa5576bd6770ea31ea2914ec6fa44ac9f78e69ad132ff00137e9542def43460670c0d69c13aca8324669ea269321c7fb47f4a4fab1fcaa6c2b923a1151c96fcf2c78a5727908d9c01f7b9fa530b3b1f947e952a46818014e718ed4aeca505d4ac6373cb1fca85451daa53934dc019e691495b6131934bb714d2d8a6efc7534ec03ce05349c75a8da403a530c993cd1615c91a8c8c542d2530cdd853b0ae4acc73d6a17723bd2fcc7a71f5a679633963ba98ae337b374e698e0f526a72706a1734137206cfa552bc189578c7cbfd6af1aa57873301e8b40884"),
                Utility.HexStringToBytes("801ac9e66c6b82a24dce0864000016404132381f0054ffff54ab92a00f4a881a9e219142286ed39ebcd1b5bd6a62bcf4ed46da762799918527bd0d16e523352e29d8a2c2bb3331b588a51525d2ed9c9ec79a8fbd302453cd39ba546a6a4ea2802c04f6a369a9714d2547522a0a23d84f6a3611c8e3e94e32a8ef4c338a3501ad0ee1b7a0a58a31164e18fd1b149f68fca9a66f73549b42b21cca36eddc71fed0e9d7fc6a7b5e26e0e783fd31fa0aabe771de93cdc76aae662b1b0b923a5381c1e6b14cb4799e82a0a3a0de80f2cbf9d1e7443aca83eac2b037b7f74d26e3e94c563a51756eaa333c7d3b3034c49e220b09171ea4e2b9e0fc82714a650c72319c629580e83ed5075f393f3a05d5bffcf64ffbeab9edc7dbf3a4dcde869858e8fed70631e727fdf54f173013feba3ffbe85731b9b1d0d1bcfbd0163a913427fe5ac7ff007d0a63dc463a3a9ff810ae677fd68f30fbd034748668f190c9cfa11514b70369c3027eb581e67b9a3cc38ea6958ae6b6c8d9331231511663d2b2f79f5346f3ea6958398d0604f079a31c702b3f71f5a37fbd3b05cd152c3b60d5db6bb652339ac2de7fbc68dedfde340ae75f0dd2b364b01f8d5cf36271f7c67eb5c2ef6c1f98fe74798d8fbe7f3a7a05cec4baab7de1f9d23cc3b11f9d71fe637f7cfe749e637f7cfe74ac87cc755e70276f269a5ce7815cbf9aff00df3f9d1e6bff007cfe740731d2b39eb511973c0ae7fcd7fef9fce8f3e41d1cd02b9bdb656e8a7f1a4f2a4279e2b105d4dd9cd385e5c0e923502b9b62203a827ea69c32bc04c0fad627dbee78fdeb63eb4a2fe7fef9a00d92c7fbb4c2c7d2b306a32f7c7e54e1a8b13caf14845e2c7d2a176f6a87edc87b1a5fb446dfc58a000b7354aeff00e3e5b9cf03f955ddc846778155e4b69657670cadec0d302a8ab76e32bef9a85ada6542761fcc1abba0f9c9a86d21d4321e08233fe7142436f4145bccc462190e7d10d4c34ebb38c40dcfa903f9d6e07dc33bb20d2f9880805c027a735b7b331e731d747ba23398c7b16ff01522e88f8f9a7507d00cd69fda2219fde29c7a734a6e23c9037363d14d57b342e7673bad699f65b68e60fbfe6dadc63af4fe46b1fd2bafd7e30fa5cc7fb8430fcf1fd6b901d2a6714b62e2ee28352a9a82a5435051ffd3e28c92375e3f1a72c13b8cac6e47a8535d0c70c51f31c6aa71d40c549bb91ce695c0c05d32e588cc640f52c2ac268929fbcf18f71935b05801c9a703d7b5176332468673cce07fdb3ffebd4aba1c381ba57fc0015a4082dc7ad29231eb4aec0cf1a25be4fef66cfd47f851fd8b075f326c7d473fa568839efd4e7eb485875cf5e28bb028ae8d6ffdf94ffc0850ba3db02325cfb16abe0f1cf1c7eb46e19c03faf7a2e053fec7b3233b1bf163c528d26c87fcb1e3fde273fad5cce013499ec3a0a40561a65a60fee138f5a71d36cc83fe8e9cf3c2e3356437048e734bbb9c023f2a00a8747b23d2118ea4ee34d3a359124f94718e30c7fce6af170727d4d359f923a7e14c0a2745b5ec1c64766a61d0edcf49261f423fc2b44b020e467f1a370ec3b73c52bb03306830923f7b311f5193fa507408ff008677c7d2b5377cd8e3fc68046d04019ce3a51760639d0307fe3e0f5feed29f0f92702eb07d3cbcff005ad72d8c734a1b04f1f951760629f0fb6de2eb27feb9ff00f5e81a0371fe94bff7efff00af5b6a49c81d7d3d7f0a427e5f5c1efda8bb0315bc3edc95b907d3f77ffd7a4fec0931cdc203feed6e9249e831fca82df3727f1a2ec2c617f6036de6e577771b334dfec1971feb90ff00c06b7f23247f5c1c527054e401ebe868bb0b187fd832647fa427fdf34cfec2b8c91e647f956f1395cf71d7d7ff00af4a5b1d7eb47330b1cf7f62dce0fcd167f1ff000a43a25d7f7a13f427fc2ba1eadd7ebeb4700e32083d3068e660738744bbf48b1fef527f62dd7fd32fccff00857464e78eb9a550303a1039e3a8ff000a39981ccb68d7833b62538ff6a9ada4de29c0b7cfd18574fbb3ce707d"),
                Utility.HexStringToBytes("801ac9e76c6b82a24dce086400001bd04132381f0054ffff85030dce3af5c7f9e69f330396fecbbcff009f7f6cef1fe34d6d36ed5771b67c7b735d59c6d05719f6ff0039147b8e413f9d2e6607206d2e075b79bfef83482de563858a427d94d763c03f30ea70783cff009fad2329f9b8c8cf6ff3c53e6038b23071bb9f7a073fc42bb32028278fa8fe9f9d364860914ac912360e4ee03147301c7e1bd47e7461c73b0e2baa7d2ecdc11e4201d7e5183fa74a8a4d12c88e2364279dc18f1ec339a7703992e47506944b8ade7d023c9549e507ae1b0703dfa540de1f982b32cd1be0e065719a77423316e4af4623f1a992f39c9623f5a7cba45d267300600e32adfe3555ed5d090d1c898eb95e28d00d08eed41182bf435723be242ef8d5c0ea76e78fa8ff000ae7f61fe1707f1a50f2c67b8aa52627147427517ce638d703d39a9e1fb7dd10d0a0dbdd83ae07d7935cf25e927f78037b9ebf9d5b86ec160cb294707209ea3f11cd5a9bea4b876352f34bbe7b1b89279630150b6d5c9c81cfb7a5727d09ae98ead76b6cf1485648e4429b98648041ee3fad73d2dbc91927195f5155269ad184535b915394d32941e6b328eb81c9e7ad0c493d3005333b48f5a5393cf4ac8a1e0f1803da80c0672718a457ce00eb8fd68c6d6c1c03400e624f183e94aac074391d78a61048f9793d739a0719dddbb5003893df81fcc53b77503e94d6249270703ae2856caf5e6801c0f383c13eb4a5b23233c9a631258f181d3e94bb8703de801c080682491c67ad34924fa0ed9ed499e3a8a0078c0c671d78f634fdc319071fd2a33c92070befdbd2954e01e4673cfbd20149ec4633d68ddd71ce29adf4edc8c5341e4f20e3bfb5031c09db9c7ff5c53830ce3af14c6ced18ff00eb520e1b9c0e7a1ed401206279e7af73cd05f83fa8a667e66ef46e3819cff5fc6801e4e48200c7af6a507040e83b7b546241f29c8ebd694fde3c0f704d004b920b0207d2807d704fbff2a8f76dc7a7ad286c820f4ee3fc2801fb8ed3c1e3f3146e20e72067d075a8f3f2f3d47bd0082bc751d7d68025c9519c0c0ec29b9e71d41e462985896c82077e074a4248f4fa0e38a00933c0c93814bc15c3753ce7b1a8b782777233dc52a92b9e4fe9400fce579f5e8687624024f43d7b7d298cc41240c81edc8a4073c7eb9eb40123b639dbc93d3b1fa1a42dc7e3d4f6a8b7018c67d7ad2861b704f03f4a404c5b92412013f518a69620f20e31eb9fcaa2c83e871dc77a524b160464d004aac738e718edda9524c703827be3afe1500fbb8186f6ef406e80f071d718e280262c071b8633c7b0a50c57db38c7afe750863b71ce3a70339a52db94ed3c0edd87f853026dfb32a3b0eb4abc0190760007ff005ea0c83c7a7bf7a15ce57691bb9ef4089f714db938f46ec3f1a787c050ebfc59e7f9feb55f76541381923a7434e2323e6c8cf43d680278c0753eb9c93ffd6a4ddc9504039edfdda89986e3f31ce00191d7f1c5383827691c85e49a603976b1c918e4914f110703e501b39c7f9e690380c0e15c0e01009efe9fe14c42a141182231cfa1cd160209f4f86600cb10c919271822a94ba14441314af1f190a791f8e6b5d58ab67396c64e38fd69f20575f97ef018c74383fa53423959f48b88c9fdd895473943827f0aa0d11562012187f0b8da6bb8d87e765e8081c7503fa77a867b582e130f1ab7381c0e0d3b81c724d2c0dd4a91d8d598ee6373f30dadea3a1fc2b46e742658cbdbc99033f238e3f0f4ac79ad9e23878da327b3743f434f40259ed1586e52a323823a1aa52c4f0b61d48ee0fad4892bc4719fc2adc7224f90e47cdd41ef4fd446cb0c739a71f99381c53412c33d31edf952efc1da41e0f6a82814ede0f5f6a736493edc1a8cf049c71ed4e0410719e9da801e8d92077e6939c64d35720f23f3f5a18ee5c8f5e78e2901296183eb8ed4ce411ffeacd11800f6fc4d0ec00033db38ce6801c5861b9ce0fa522821b9047a8e942b673efdff00cfd689093ce723bfad00389fbd83c8ee"),
                Utility.HexStringToBytes("801ac9e86c6b82a24dce0864000021604132381f0054ffff29809cf208cf5a58c1cfbe2918e5474f7f5340c90900f50303b7ad301fc7d7de923e08e727a7b9a5246d3e94843b2318271fe1ef519e5b3bb27d690151820f4e413daa50704f1ce3383dfeb40c030dbcff003fe54d6383f78600e7da987193919f5f7a72b0080824e3f4a00721c9e483c704f4a0b7cbc7ff00ae98f82402303d40e9426327f223d680154eeedd7af1d69e18000e3e9e94de0ab63b7e42980e7a1c8ee280243cb9c67fc69439047cc39e326914fc839e0704ff004a463d323231d3d2801c0eec8c67db348391d0f1de9bd0a923a77a729e573c1cf1ed40006033c741d051bfe5c839c77cf7a8b383939f63e94e04e7af27fce6801ed9eb819c76ff000a376477f7a6e415209edd7d6901e71923dc7340126492194819efe94849c64773e94d5cedc1ce3a9e29a486f73fca801e4e79009c0c6476a5e841c9c7d3ad478e38cfa641a556c704e0e38e2900a4e307bf5c1a52475c8e4f2a4734de1b006d38e7d0d1b77200d8393c022801dea768ff000a463823ae3b1f5a692791dcfbf5a771c020f03a6320d300ce0eee55ba93d8d3b39071c81d7de9a303033927d0d3771c1e7a9ce08a00909fe2ce7233834310d8fba7f2c8a6ab10720820f1d7a51bb8033f28f5ef480783f2eef43c7a5395d87cc99ce3271d0d3385383953df1d0fd7f3a1beff00cb8c91c907ad301fbb71560003d80ea7f0a50c0a8504ab13df8cfe3516edbf373c76ed4e0d8c907728c67bfa714c097ccdc18a9f9b39c63d7ffd74eddbdb19c01824f539a889076b1e98fc6804ed073b8e727d47e0681165be642cdc8c70cbd07d452093707604718404700d40aeca03a3721b1c0c1a95b9662304a9ee319e29889e370b8e80a1009ce0d48c55dc4ab8dc0fcc71d7ae2abe496f94ed738c64f3f5fd280fb8657e5ded9233d053027552429009e39f424d45736f0dc26d28ae9807079ea6a5120c676f04f049e73fe453c6d0c791e5b1079e0e07e9e94c472fa968725b92d0e5e33fc27923e86b1991a363d4107041ea3eb5e88f18e55c72727247e23f4ac5d5b465955a48815914f271d78e845303ffd4cb560a40c52105464f4f6a4db824ff002a19b23a9f5a4317702a7927e94b19c373f2fd7b5317af4c7f4a6b9dc473c7f2a4048f20ee7ea33d2951b8e4e3d69a8db547603f4a6b6ede724803d0d003df1bfb7b63f953d1be51fdd5fd334c183c6d19e878ff003cd34b367938faf6a007b6edc4743e9d734e0dc7d29a5549e87d7af4a6ef3f7474fe540c7b90af8190476a55da5724027b7a9a68f9b07b9edea69378048cf1d33400e39180840efed40391f363ebe946dcae492077f6fa5231d87819efcf5a4039c80bcfca47f9c9a6862cc37127dbd6932581c30ce738c6734608008391d381d3da8024e074e9ea0ff2a8cb1e9b980fe468dc4f40403c73d076a5c36776738e3eb4301c06465b24e7079eb48cdb700120fd78a4dc5491839c601a4fbcab818c718ef4002b6e6e724771520000ebd3a9cff2a8cee538ea41c714bbf041008fe94009bf820371dfda9f8054f4e3bfad33e604fcdd3be282db08c83d3f2a007b10067001e3a7614de49c1c7b107ad0a49dcbbb6f39e475a369e871827818a007646e0c00cf6148f8c8c67ea2a32d80474cf7a5cee1d402380290c50dce7f8fe9d69fbb83cf1dcfad3303180d9c75e7a5264e36f3c7e3400e6fbdf7707d451d08e483ee38a6e339ebbb3d7b51c01c1200e99cd003b760f3f80a187ce413c67a8a66e3d33c9e87229c02b0208031d4e280149c907391eb8a52c71918dc69300a9c2803a0038c53377a962bf81ff3d68024638dc06719e845378041c63bf3da80d9ebca839c52a96ce3243376f6a0042c020fe74e2d90cc36819ec699807d149efd01a0390c4e39fae41a0093072a7a9efc62901246dc60fe7e94d07d4707a9a0b751c11f4cfe54087839dca73bb8007229c3a818071c9f5a8b386c838c0ef9a37108304fbf7a6048b9c819cfd383"),
                Utility.HexStringToBytes("809ac9e96c6b82a24dce0864000026f04132381f0054ffff481b0719e1874231499ca9618ebda82795e79c753eb400e0c31924851c53f6805b772477fbc076a88825429c03ee38fce8520e53680c4f0739cff9340126ec12571d09f94ff4a914e0850393f787d7dbf1a8b25d89e0f1dc75a7249c120023ae1b0476fc4531132c980801051b9c6738eb4e472a55f8da320f383eff00ceab2315249c9078cf5ebc9e6a556eaa51b6800291f5a7702c7982351b4038041040cfe1d8d4e842ca533b9540507b727bd53040493700a171bba0cf3edd4d488320861b4eecf3c8ce71d7ff00d754988bb9f31594e558824853cfb63d6a53fbccb003ab1c0cf5edfe78aa81cab938d8103753c364f1fa5588a45e8003b4052c07e7f514d12cffd9")
            };

            Rtp.JpegFrame restartFrame = new Rtp.JpegFrame();

            foreach (byte[] binary in jpegPackets) restartFrame.Add(new Rtp.RtpPacket(binary));

            if (!restartFrame.Complete) throw new Exception("Not Complete");
            else using (System.Drawing.Image jpeg = restartFrame)
                {
                    jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }

        }
    }
}
