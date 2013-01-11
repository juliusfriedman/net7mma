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

            Console.WriteLine("Press a Key to Start Next Test");
            Console.ReadKey();
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
            Console.WriteLine("Press a Key to Start Next Test");
            System.Console.ReadKey();
        }

        static void TestRtspMessage()
        {
            Rtsp.RtspRequest request = new Rtsp.RtspRequest();
            request.CSeq = 2;
            request.Location = new Uri("rtsp://aol.com");
            Rtsp.RtspResponse response = new Rtsp.RtspResponse();

            Rtsp.RtspMessage message = request;

            byte[] bytes = message.ToBytes();

            if (!(new Rtsp.RtspRequest(bytes).Location == request.Location))
            {
                throw new Exception();
            }

            //Do some testing make them to bytes, reparse. Parse temples ... etc

        }

        static void TestRtspClient()
        {
            //Make a client
            //This host uses Udp
            Rtsp.RtspClient client = new Rtsp.RtspClient("rtsp://178.218.212.102:1935/live/Stream1");
        Start:
            //Assign some events
            client.OnConnect += (sender) => { Console.WriteLine("Connected to :" + client.Location); };
            client.OnRequest += (sender, request) => { Console.WriteLine("Client Requested :" + request.Location + " " + request.Method); };
            client.OnResponse += (sender, response) => { Console.WriteLine("Client got response :" + response.StatusCode); };
            client.OnDisconnect += (sender) => { Console.WriteLine("Disconnected from :" + client.Location); };
            try
            {
                //Try to StartListening
                client.StartListening();
            }
            catch(Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Was unable to StartListening: " + ex.Message);
                Console.BackgroundColor = ConsoleColor.Black;
            }

            //Add some more events once Listening
            client.Client.RtpPacketReceieved += (sender, rtpPacket) => { Console.WriteLine("Got a RTP packet, SequenceNo = " + rtpPacket.SequenceNumber + " Channel = " + rtpPacket.Channel + " PayloadType = " + rtpPacket.PayloadType + " Length = " + rtpPacket.Length); };
            client.Client.RtpFrameChanged += (sender, rtpFrame) => { Console.BackgroundColor = ConsoleColor.Blue; Console.WriteLine("Got a RTPFrame PacketCount = " + rtpFrame.Count + " Complete = " + rtpFrame.Complete); Console.BackgroundColor = ConsoleColor.Black; };
            client.Client.RtcpPacketReceieved += (sender, rtcpPacket) => { Console.WriteLine("Got a RTCP packet Channel= " + rtcpPacket.Channel + " Type=" + rtcpPacket.PacketType + " Length=" + rtcpPacket.Length + " Bytes = " + BitConverter.ToString(rtcpPacket.Data)); };
            client.Client.RtcpPacketReceieved += (sender, rtcpPacket) => { Console.BackgroundColor = ConsoleColor.Green; Console.WriteLine("Sent a RTCP packet Channel= " + rtcpPacket.Channel + " Type=" + rtcpPacket.PacketType + " Length=" + rtcpPacket.Length + " Bytes = " + BitConverter.ToString(rtcpPacket.Data)); Console.BackgroundColor = ConsoleColor.Black; };


            Console.WriteLine("Waiting for packets... Press Q to exit");
            
            //Ensure we recieve a bunch of packets before we say the test is good
            while (Console.ReadKey().Key != ConsoleKey.Q)

            //All done
            Console.WriteLine("Exiting RtspClient Test");

            try
            {
                if (client.Connected)
                {
                    //Send a few requests if we are connected just because
                    var one = client.SendOptions();
                    var two = client.SendOptions();
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("Sending Options Success");
                    Console.WriteLine(string.Join(" ", one.GetHeaders()));
                    Console.WriteLine(string.Join(" ", two.GetHeaders()));
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            }
            catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Sending Options Failed");
                Console.BackgroundColor = ConsoleColor.Black;
            }
            
            //Print information before disconnecting

            Console.BackgroundColor = ConsoleColor.Green;

            Console.WriteLine("RtcpBytes Sent: " + client.Client.TotalRtcpBytesSent);
            Console.WriteLine("Rtcp Packets Sent: " + client.Client.TotalRtcpPacketsSent);
            Console.WriteLine("RtcpBytes Recieved: " + client.Client.TotalRtcpBytesReceieved);
            Console.WriteLine("Rtcp Packets Recieved: " + client.Client.TotalRtcpPacketsReceieved);

            Console.BackgroundColor = ConsoleColor.Black;
            
            //Calls Disconnect if the client is Connected
            client.StopListening();

            //Perform another test if we need to
            if (client.Location.ToString() != "rtsp://fms.zulu.mk/zulu/a2_1")
            {
                //Try another host (this one uses Tcp and forces the client to switch from Udp because Udp packets never arrive)
                client = new Rtsp.RtspClient("rtsp://fms.zulu.mk/zulu/a2_1");
                //client.ProtocolSwitchSeconds = 5; //Switch in 5 seconds rather than the default of 10
                Console.WriteLine("Performing 2nd Client test");
                goto Start;
            }

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

        }

        /// <summary>
        /// Tests the RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        static void TestServer()
        {
            Rtsp.RtspServer server = new Rtsp.RtspServer();

            //Create a stream which will be exposed under the name Uri rtsp://localhost/live/RtspSourceTest
            //Here are some example Rtsp Sources
            //rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov

            //H264 Stream Udp
            Rtsp.Server.Streams.RtspSourceStream source = new Rtsp.Server.Streams.RtspSourceStream("Alpha", "rtsp://fms.zulu.mk/zulu/a2_1");
            
            //If the stream had a username and password
            //source.Client.Credential = new System.Net.NetworkCredential("user", "password");
            
            //Add the stream to the server
            server.AddStream(source);

            //MPEG4 Stream -> Tcp
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Beta", "rtsp://178.218.212.102:1935/live/Stream1"));

            //H264 Stream -> Udp available but causes switch to TCP if NAT
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Gamma", "rtsp://184.72.239.149/vod/mp4:BigBuckBunny_175k.mov"));

            //H264 Stream -> Udp available but causes switch to TCP if NAT
            //Responding with 500 Internal Server Error for Setup / Play on TCP and UDP gets 400 Bad Request?
            server.AddStream(new Rtsp.Server.Streams.RtspSourceStream("Delta", "rtsp://mediasrv.oit.umass.edu/densmore/nenf-boston.mov"));

            //Start the server
            server.Start();

            //If you add more streams they will be started

            Console.WriteLine("Listening on: " + server.LocalEndPoint);

            Console.WriteLine("Active Streams :" + server.ActiveStreamCount);

            Console.WriteLine("Waiting for input................");

            while (true)
            {

                var key = Console.ReadKey();

                if (key.KeyChar == 'q') break;
                else
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }

                //server.EnableHttp();
                //server.EnableUdp();

                //TestClients();

                //server.DisableHttp();
                //server.DisableUdp();

            }

            Console.WriteLine("Stopping Server");

            server.Stop();

            Console.WriteLine("Stream Recieved : " + server.TotalStreamBytesRecieved);

            Console.WriteLine("Stream Sent : " + server.TotalStreamBytesSent);

            Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

            Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

            Console.WriteLine("Waiting for input to Exit................");

            Console.ReadKey();

        }
        
        /// <summary>
        /// Tests the Rtp and RtspClient in various modes (Against the server)
        /// </summary>
        static void TestClients()
        {
            //Call to the server in Tcp, Udp, and Http

            Rtsp.RtspClient tcp = new Rtsp.RtspClient("rtsp://localhost/live/RtspSourceTest");

            //Rtsp.RtspClient udp = new Rtsp.RtspClient("rtspu://localhost/live/RtspSourceTest");

            //Required Admin Priv
            //Rtsp.RtspClient http = new Rtsp.RtspClient("http://localhost/live/RtspSourceTest");

            Enumerable.Range(0, 100).All((i) =>
            {
                try
                {
                    tcp.Connect();
                    tcp.SendOptions();

                    //udp.Connect();
                    //udp.SendOptions();

                    //http.Connect();
                    //http.SendOptions();

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        static void TestJpegFrame()
        {            

            Rtp.JpegFrame f = new Rtp.JpegFrame(System.Drawing.Image.FromFile("video.jpg"));
            using (System.Drawing.Image jpeg = f)
            {
                jpeg.Save("source.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }


            Rtp.JpegFrame t = new Rtp.JpegFrame(f);
            using (System.Drawing.Image jpeg = t)
            {
                jpeg.Save("result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }
    }
}
