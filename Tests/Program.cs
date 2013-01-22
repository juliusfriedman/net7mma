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
            TestJpegFrame();
            System.Console.Clear();
            TestRtspMessage();
            System.Console.Clear();
            TestRtpPacket();
            System.Console.Clear();
            TestRtcpPacket();
            System.Console.Clear();
            TestRtpDump();
            System.Console.Clear();
            TestSdp();
            System.Console.Clear();
            TestRtspClient();
            System.Console.Clear();
            TestServer();
        }

        private static void TestRtcpPacket()
        {
            Console.WriteLine("RtcpTest");
            byte[] example = new byte[] { 0x80, 0xc8, 0x00, 0x06, 0x43, 0x4a, 0x5f, 0x93, 0xd4, 0x92, 0xce, 0xd4, 0x2c, 0x49, 0xba, 0x5e, 0xc4, 0xd0, 0x9f, 0xf4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Rtcp.RtcpPacket asPacket = new Rtcp.RtcpPacket(example);
            Rtcp.SendersReport sr = new Rtcp.SendersReport(asPacket);
            Console.WriteLine(sr.SynchronizationSourceIdentifier);//1928947603
            Console.WriteLine(sr.NtpTimestamp);//MSW = d4 92 ce d4, LSW = 2c 49 ba 5e
            sr.NtpTimestamp = sr.NtpTimestamp;//Ensure setting the value through a setter is correct
            //Ensure the utility function works...
            Console.WriteLine(sr.RtpTimestamp);//3302006772
            
            //Verify SendersReport byte for byte
            var output = sr.ToPacket().ToBytes();//should be exactly equal to example
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
            for (int i = asPacket.Length; i >= 0; --i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

            int offset = output.Length + Rtcp.RtcpPacket.RtcpHeaderLength;

            //Verify Source Description byte for byte
            output = sd.ToBytes();
            for (int i = 0; i < output.Length; i++, offset++)
            {
                if (example[offset] != output[i]) throw new Exception();
            }
            Console.WriteLine("RtcpPacket Test passed!");
            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();                        
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

            Console.WriteLine("RtpDump Test passed!");
            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();

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
            Rtp.RtpPacket p = new Rtp.RtpPacket();
            p.TimeStamp = 987654321;
            p.SequenceNumber = 7;
            p.Marker = true;
            p = new Rtp.RtpPacket(p.ToBytes());
            Console.WriteLine(p.TimeStamp);
            Console.WriteLine(p.SequenceNumber);
            Console.WriteLine("Waiting for input to Exit................ (Press any key)");
            Console.ReadKey();
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
                throw new Exception();
            }

            Console.WriteLine("RtspMessage Test passed!");

            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();

        }

        static void TestRtspClient()
        {

            Console.WriteLine("RtspClient Test. Press a key to continue. Press Q to Skip");
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
                if (client.Location.ToString() != "rtsp://fms.zulu.mk/zulu/a2_1")
                {
                    //Do another test
                    Console.WriteLine("Press a Key to Start 2nd RtspClient Test (Q to Skip)");
                    if (System.Console.ReadKey().Key != ConsoleKey.Q)
                    {

                        //Try another host (this one uses Tcp and forces the client to switch from Udp because Udp packets never arrive)
                        //We will not specify Tcp we will allow the client to switch over automatically
                        client = new Rtsp.RtspClient("rtsp://fms.zulu.mk/zulu/a2_1");
                        //Switch in 5 seconds rather than the default of 10
                        client.ProtocolSwitchSeconds = 5;
                        Console.WriteLine("Performing 2nd Client test");
                        goto StartTest;
                    }
                }

                
            }

            Console.WriteLine("Test Complete");
            Console.WriteLine("Press a Key to Start Next Test");
            System.Console.ReadKey();
        }

        static void TestRtpPackets()
        {
            //Make and (de)serialize some RtpPackets
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

            Console.WriteLine("SDP Test passed!");

            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();

        }

        static bool udpEnabled, httpEndabled;

        /// <summary>
        /// Tests the RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        static void TestServer()
        {
            //Setup a RtspServer on port 554
            Rtsp.RtspServer server = new Rtsp.RtspServer();

            //The server will take in RtspSourceStreams and make them available locally

            //H264 Stream Tcp Exposed @ rtsp://localhost/live/Alpha through Udp and Tcp
            Rtsp.Server.Streams.RtspSourceStream source = new Rtsp.Server.Streams.RtspSourceStream("Alpha", "rtsp://fms.zulu.mk/zulu/a2_1", Rtsp.RtspClient.ClientProtocolType.Tcp);
            
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
            Console.WriteLine("Press 'T' to Perform Load Test on RtspServer");

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Q) break;
                else if (keyInfo.Key == ConsoleKey.H)
                {
                    Console.WriteLine("Enabling Http");
                    server.EnableHttp();
                    httpEndabled = true;
                }
                else if (keyInfo.Key == ConsoleKey.U)
                {
                    Console.WriteLine("Enabling Udp");
                    server.EnableUdp();
                    udpEnabled = true;
                }
                else if (keyInfo.Key == ConsoleKey.T)
                {
                    Console.WriteLine("Performing Load Test");
                    LoadTest(httpEndabled, udpEnabled);
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
        static void LoadTest(bool http = true, bool udp = true)
        {
            //100 times about a GB in total

            //Get the Degrees Of Parallelism
            int dop = 0;

            if (httpEndabled) dop += 2;
            if (udp) dop += 2;
            dop += 3;//Tcp

            //Test the server
            ParallelEnumerable.Range(1, 100).AsParallel().WithDegreeOfParallelism(dop).ForAll(i =>
            {
                //Create a client
                if (http && i % 2 == 0) 
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
                else if (udp && i % 3 == 0) 
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

                            Console.WriteLine("Test passed");

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
            //Create a JpegFrame from a Image
            Rtp.JpegFrame f = new Rtp.JpegFrame(System.Drawing.Image.FromFile("video.jpg"));
            //Save the JpegFrame as a Image
            using (System.Drawing.Image jpeg = f)
            {
                jpeg.Save("source.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            //Create a JpegFrame from an existing RtpFrame
            Rtp.JpegFrame t = new Rtp.JpegFrame(f);
            //Save JpegFrame as Image
            using (System.Drawing.Image jpeg = t)
            {
                jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            Console.WriteLine("Success video.jpg Encoded and saved as result.jpg");

            Console.WriteLine("Waiting for input to Exit................ (Press any key)");

            Console.ReadKey();

        }
    }
}
