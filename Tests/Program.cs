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
            RunTest(TestRtpClient);
            RunTest(TestRtspClient);
            RunTest(TestServer);
        }      

        /// <summary>
        /// Tests the RtpClient.
        /// </summary>
        private static void TestRtpClient()
        {
            Rtp.RtpClient sender = Rtp.RtpClient.Sender(Utility.GetFirstV4IPAddress());

            //Create a Session Description
            Sdp.SessionDescription SessionDescription = new Sdp.SessionDescription(1);

            //Add a MediaDescription to our Sdp on any port 17777 for RTP/AVP Transport using the RtpJpegPayloadType
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 17777, Rtsp.Server.Streams.RtpSource.RtpMediaProtocol, Rtp.JpegFrame.RtpJpegPayloadType));

            System.Net.IPAddress localIp = Utility.GetFirstV4IPAddress();

            Rtp.RtpClient receiver = Rtp.RtpClient.Receiever(Utility.GetFirstV4IPAddress());

            //Create and Add the required TransportContext's

            Rtp.RtpClient.TransportContext sendersContext = new Rtp.RtpClient.TransportContext(0, 1, (uint)DateTime.UtcNow.Ticks, SessionDescription.MediaDescriptions[0]),
                receiversContext = new Rtp.RtpClient.TransportContext(0, 1, (uint)DateTime.UtcNow.Ticks, SessionDescription.MediaDescriptions[0]);

            //Find open ports, 1 for Rtp, 1 for Rtcp
            int rtpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17777, false), rtcpPort = Utility.FindOpenPort(System.Net.Sockets.ProtocolType.Udp, 17778);

            receiversContext.InitializeSockets(localIp, localIp, rtpPort, rtcpPort, rtpPort, rtcpPort);

            receiver.AddTransportContext(receiversContext);

            //Connect the reciver
            receiver.Connect();

            sendersContext.InitializeSockets(localIp, localIp, rtpPort, rtcpPort, rtpPort, rtcpPort);

            sender.AddTransportContext(sendersContext);

            //Connect the sender
            sender.Connect();

            //Send an initial report
            sender.SendSendersReports();
            receiver.SendReceiversReports();

            //Make a frame
            Rtp.JpegFrame testFrame = new Rtp.JpegFrame(System.Drawing.Image.FromFile("video.jpg"), 100, sendersContext.SynchronizationSourceIdentifier, 1);
            
            //Send it
            sender.SendRtpFrame(testFrame);

            //Send another report
            sender.SendSendersReports();

            //Send the receivers report
            receiver.SendReceiversReports();

            //Send a goodbye
            sender.SendGoodbyes();

            //Check values

            if (sender.TotalRtpPacketsSent != testFrame.Count) throw new Exception("Did not send entire frame");

            //Measure QoE / QoS based on sent / received ratio.

            Console.WriteLine("Since : " + sendersContext.SendersReport.Sent);
            Console.WriteLine("-----------------------");
            Console.WriteLine("Sender Sent : " + sendersContext.SendersReport.SendersPacketCount + " Packets");

            //Determine what is actually being received by obtaining the TransportContext of the receiver
            
            Console.WriteLine("Since : " + receiversContext.RecieversReport.Created);
            Console.WriteLine("-----------------------");
            Console.WriteLine("Receiver Lost : " + receiversContext.RecieversReport.Blocks[0].CumulativePacketsLost + " Packets");

            Console.WriteLine("Test Passed");
        }

        private static void RunTest(Action test)
        {
            System.Console.Clear();
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine("About to run test: " + test.Method.Name);
            Console.WriteLine("Press Q to skip or any other key to continue.");
            Console.BackgroundColor = ConsoleColor.Black;
            if (Console.ReadKey().Key == ConsoleKey.Q)
            {
                return;
            }
            else
            {
                try
                {
                    test();
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("Test Passed!");
                    Console.WriteLine("Press a key to continue.");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("Test Failed!");
                    Console.WriteLine("Exception.Message: " + ex.Message);
                    Console.WriteLine("Press a key to continue.");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ReadKey();
                }
            }
        }

        private static void TestRtcpPacket()
        {
            Console.WriteLine("RtcpTest");
            Console.WriteLine("--------");
            byte[] example = new byte[] { 0x80, 0xc8, 0x00, 0x06, 0x43, 0x4a, 0x5f, 0x93, 0xd4, 0x92, 0xce, 0xd4, 0x2c, 0x49, 0xba, 0x5e, 0xc4, 0xd0, 0x9f, 0xf4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Rtcp.RtcpPacket asPacket = new Rtcp.RtcpPacket(example);
            Rtcp.SendersReport sr = new Rtcp.SendersReport(asPacket);
            Console.WriteLine(sr.SynchronizationSourceIdentifier);//1928947603
            Console.WriteLine(sr.NtpTimestamp);//MSW = d4 92 ce d4, LSW = 2c 49 ba 5e
            sr.NtpTimestamp = sr.NtpTimestamp;//Ensure setting the value through a setter is correct
            if (sr.RtpTimestamp != 3302006772) throw new Exception("RtpTimestamp Invalid!");
            
            //Verify SendersReport byte for byte
            var output = sr.ToPacket().ToBytes();//should be exactly equal to example
            for (int i = 0; i < output.Length; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            //Recievers Report and Source Description
            example = new byte[] { 0x81,0xc9,0x00,0x07,0x69,0xf2,0x79,0x50,0x61,0x37,0x94,0x50,0xff,0xff,0xff,0xff,
                                0x00,0x01,0x00,0x52,0x00,0x00,0x0e,0xbb,0xce,0xd4,0xc8,0xf5,0x00,0x00,0x84,0x28,
                                0x81,0xca,0x00,0x03,0x69,0xf2,0x79,0x50,0x01,0x06,0x4a,0x61,0x79,0x2d,0x50,0x43,
                                0x00,0x00,0x00,0x00
            };

            //Could check for multiple packets with a function without having to keep track of the offset with the RtcpPacket.GetPackets Function
            Rtcp.RtcpPacket[] foundPackets = Rtcp.RtcpPacket.GetPackets(example);
            Console.WriteLine(foundPackets.Length);

            //Or manually for some reason
            asPacket = new Rtcp.RtcpPacket(example); // same as foundPackets[0]
            Rtcp.ReceiversReport rr = new Rtcp.ReceiversReport(asPacket);
            Console.WriteLine(rr.SynchronizationSourceIdentifier);//1777498448
            Console.WriteLine(rr.Blocks.Count);//1
            Console.WriteLine(rr.Blocks[0].SynchronizationSourceIdentifier);//1631032400
            Console.WriteLine(rr.Blocks[0].FractionLost);//255/256 0xff
            Console.WriteLine(rr.Blocks[0].CumulativePacketsLost);//-1, 0xff,0xff,0xff
            Console.WriteLine(rr.Blocks[0].ExtendedHigestSequenceNumber);//65618, 00, 01, 00, 52
            Console.WriteLine(rr.Blocks[0].InterArrivalJitter);//3771
            Console.WriteLine(rr.Blocks[0].LastSendersReport);//3470051573

            //next packet offset by Length + RtcpHeader
            asPacket = new Rtcp.RtcpPacket(example, asPacket.Length + Rtcp.RtcpPacket.RtcpHeaderLength); //same as foundPackets[1]
            Rtcp.SourceDescription sd = new Rtcp.SourceDescription(asPacket); //1 Chunk, CName

            //Verify RecieversReport byte for byte
            output = rr.ToPacket().ToBytes();//should be exactly equal to example
            for (int i = asPacket.PacketLength; i >= 0; --i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            int offset = output.Length;// +Rtcp.RtcpPacket.RtcpHeaderLength;

            //Verify Source Description byte for byte
            output = sd.ToPacket().ToBytes();
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

            sd = new Rtcp.SourceDescription(0x1AB7C080); //4 Bytes
            sd.Add(new Rtcp.SourceDescription.SourceDescriptionItem(Rtcp.SourceDescription.SourceDescriptionType.CName, "FLABIA-PC")); // ItemType(1), Length(1), ItemValue(9), End(1) =  12 Bytes
            asPacket = sd.ToPacket(); // Header = 4 Bytes

            if (asPacket.Length != 16) throw new Exception("Invalid Length");
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

        static void TestRtspClient()
        {

            Console.WriteLine("Test #1. Press a key to continue. Press Q to Skip");
            if (Console.ReadKey().Key != ConsoleKey.Q)
            {

                //Make a client
                //This host uses Udp but also supports Tcp if Nat fails
                Rtsp.RtspClient client = new Rtsp.RtspClient("rtsp://178.218.212.102:1935/live/Stream1");
            StartTest:
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
                    client.Client.RtcpPacketReceieved += (sender, rtcpPacket) => { Console.BackgroundColor = ConsoleColor.Green; Console.WriteLine("Sent a RTCP packet Channel= " + rtcpPacket.Channel + " Type=" + rtcpPacket.PacketType + " Length=" + rtcpPacket.Length + " Bytes = " + BitConverter.ToString(rtcpPacket.Payload)); Console.BackgroundColor = ConsoleColor.Black; };

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

                //All done
                Console.WriteLine("Exiting RtspClient Test");

                //Perform another test if we need to
                if (client.Location.ToString() != "rtsp://fms.zulu.mk/zulu/alsat_2")
                {
                    //Do another test
                    Console.WriteLine("Press a Key to Start Test #2 (Q to Skip)");
                    if (System.Console.ReadKey().Key != ConsoleKey.Q)
                    {

                        //Try another host (this one uses Tcp and forces the client to switch from Udp because Udp packets usually never arrive)
                        //We will not specify Tcp we will allow the client to switch over automatically
                        client = new Rtsp.RtspClient("rtsp://fms.zulu.mk/zulu/alsat_2");
                        //Switch in 5 seconds rather than the default of 10
                        client.ProtocolSwitchSeconds = 5;
                        Console.WriteLine("Performing 2nd Client test");
                        goto StartTest;
                    }
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

            //H264 Stream Tcp Exposed @ rtsp://localhost/live/Alpha through Udp and Tcp
            Rtsp.Server.Streams.RtspSourceStream source = new Rtsp.Server.Streams.RtspSourceStream("Alpha", "rtsp://fms.zulu.mk/zulu/alsat_2", Rtsp.RtspClient.ClientProtocolType.Tcp);
            
            //If the stream had a username and password
            //source.Client.Credential = new System.Net.NetworkCredential("user", "password");
            
            //Add the stream to the server
            server.AddStream(source);

            //MPEG4 Stream Tcp Exposed @ rtsp://localhost/live/Beta through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Beta", "rtsp://178.218.212.102:1935/live/Stream1", Rtsp.RtspClient.ClientProtocolType.Tcp));

            //H264 Stream -> Udp available but causes switch to TCP if NAT Fails - Exposed @ rtsp://localhost/live/Gamma through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Gamma", "rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov"));

            //H264 Stream -> Udp available but causes switch to TCP if NAT Fails - Exposed @ rtsp://localhost/live/Delta through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Delta", "rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov"));

            //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/Pics through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.JpegRtpImageSource("Pics", System.Reflection.Assembly.GetExecutingAssembly().Location) { Loop = true });

            //Local Stream Provided from pictures in a Directory - Exposed @ rtsp://localhost/live/SamplePictures through Udp and Tcp
            server.AddStream(new Rtsp.Server.Streams.JpegRtpImageSource("SamplePictures", @"C:\Users\Public\Pictures\Sample Pictures\") { Loop = true });

            //Start the server
            server.Start();

            //If you add more streams they will be started once the server is started

            Console.WriteLine("Listening on: " + server.LocalEndPoint);

            Console.WriteLine("Active Streams :" + server.ActiveStreamCount);

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
            }

            server.DisableHttp();

            server.DisableUdp();

            Console.WriteLine("Stream Recieved : " + server.TotalStreamBytesRecieved);

            Console.WriteLine("Stream Sent : " + server.TotalStreamBytesSent);

            Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

            Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

            Console.WriteLine("Stopping Server");            

            server.Stop();

            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();

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
        }
    }
}
