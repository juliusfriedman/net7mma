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
            TestRtspMessage();
            TestSdp();
            TestServer();
            TestEncoderDecoder();
        }

        static void TestRtspMessage()
        {
            Rtsp.RtspRequest request = new Rtsp.RtspRequest();
            Rtsp.RtspResponse response = new Rtsp.RtspResponse();

            Rtsp.RtspMessage message;

            //Do some testing make them to bytes, reparse. Parse temples ... etc

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

            Sdp.SessionDescriptionLine mpeg4IodLine = sd.Lines.Where(l => l.Type == 'a' && l.ToString().Contains("mpeg4-iod")).FirstOrDefault();

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
            //From the RtspSource rtsp://1.2.3.4/mpeg4/media.amp
            Rtsp.RtspSourceStream source = new Rtsp.RtspSourceStream("RtspSourceTest", "rtsp://1.2.3.4/mpeg4/media.amp");
            //If the stream had a username and password
            //source.Client.Credential = new System.Net.NetworkCredential("user", "password");
            
            //Add the stream to the server
            server.AddStream(source);

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

                TestClients();

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
        }

        static void TestEncoderDecoder()
        {
            //Create a JpegFrame from a source... we will have to setup a RtspServer

            //Make a list of a few jpegs..

            //Create a RTPFrame from each Jpeg

            //Subclass RtspStream to provide the source as the RTPFrames

            //Send the Frames to each client that connects
        }
    }
}
