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
            TestServer();
            TestEncoderDecoder();
        }

        /// <summary>
        /// Tests the RtspServer by creating a server, loading/exposing a stream and waiting for a keypress to terminate
        /// </summary>
        public static void TestServer()
        {
            Rtsp.RtspServer server = new Rtsp.RtspServer();

            //Create a stream which will be exposed under the name Uri rtsp://localhost/live/RtspSourceTest
            //From the RtspSource rtsp://1.2.3.4/mpeg4/media.amp
            Rtsp.RtspStream source = new Rtsp.RtspStream("RtspSourceTest", "rtsp://1.2.3.4/mpeg4/media.amp");
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
                
            }

            Console.WriteLine("Stopping Server");

            server.Stop();

            Console.WriteLine("Stream Recieved : " + server.TotalStreamBytesRecieved);

            Console.WriteLine("Stream Sent : " + server.TotalStreamBytesSent);

            Console.WriteLine("Rtsp Recieved : " + server.TotalRtspBytesRecieved);

            Console.WriteLine("Rtsp Sent : " + server.TotalRtspBytesSent);

            Console.WriteLine("Waiting for input to Exit................");

            Console.ReadKey();

        }

        public static void TestEncoderDecoder()
        {
            //Create a JpegFrame from a source... we will have to setup a RtspServer

            //Make a list of a few jpegs..

            //Create a RTPFrame from each Jpeg

            //Subclass RtspStream to provide the source as the RTPFrames

            //Send the Frames to each client that connects
        }

    }
}
