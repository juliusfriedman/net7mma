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

            Rtsp.RtspServer server = new Rtsp.RtspServer();

            Rtsp.RtspStream source = new Rtsp.RtspStream("nh7", "rtsp://asti-video.nesl.com/live/p_NH7_CAM_1");
            server.AddStream(source);

            //Handle multiple AS params in the SDP
            //Also handle bad reqeust to setup

            //source = new Rtsp.RtspStream("youtube", "rtsp://v4.cache3.c.youtube.com/CjgLENy73wIaLwmuxcpmJBuHhRMYDSANFEIJbXYtZ29vZ2xlSARSB3JlbGF0ZWRg7I3t9PCLl6xQDA==/0/0/0/video.3gp");
            //server.AddStream(source);

            //source = new Rtsp.RtspServer.RtspStream("c7", "rtsp://asti-video.nesl.com/live/p_C7");
            //server.AddStream(source);

            server.Start();

            Console.WriteLine("Listening on: " + server.LocalEndPoint);

            Console.WriteLine("Active Streams :" + server.ActiveStreamCount);

            Console.WriteLine("Waiting for input................");

            Console.ReadKey();

            Console.WriteLine("Stopping Server");

            server.Stop();

            Console.WriteLine("Stream Recieved : " + server.TotalStreamBytesRecieved);

            Console.WriteLine("Stream Sent : " + server.TotalStreamBytesSent);

            Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

            Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

            Console.WriteLine("Waiting for input to Exit................");

            Console.ReadKey();

        }
    }
}
