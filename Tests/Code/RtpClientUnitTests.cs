#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 16 Dec 2014 `exterminator`  Created @ http://net7mma.codeplex.com/workitem/17245
 28 Dec 2014    Julius Friedman juliusfriedman@gmail.com  modified to ensure the Payload type of the generated packets was the same as indicated in the MediaDescription and changed the range of the Issue2 to include more bytes.

 * //Todo, allow for automatic testing when not attached, keep the packet information sent so it can be verified upon receiption.
 
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
using System.Linq;
using System.Collections.Generic;

public class RtpClientUnitTests
{
    #region Issue 17245

    public static class TestProcessFrameData
    {
        private const int _senderSSRC = 0x53535243; //  "SSRC"
        private const int _timeStamp = 0x54494d45;  //  "TIME"

        private class TestFramework
        {
            private static System.Net.EndPoint _rtspServer;

            private static System.Net.Sockets.Socket _listenSocket;

            private System.Net.Sockets.Socket _sender,
                           _receiving;

            private Media.Rtp.RtpClient _client;

            static TestFramework()
            {
                _rtspServer = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 10554);

                //  Create a (single) listening socket.
                _listenSocket = new System.Net.Sockets.Socket(_rtspServer.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                _listenSocket.Bind(_rtspServer);
                _listenSocket.Listen(1);
            }

            public TestFramework()
            {
                //  Create a receiving socket.
                _receiving = new System.Net.Sockets.Socket(_rtspServer.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                //  Connect to the server.
                System.IAsyncResult connectResult = null;
                connectResult = _receiving.BeginConnect(_rtspServer, new System.AsyncCallback((iar) =>
                {
                    try { _receiving.EndConnect(iar); }
                    catch { }
                }), null);

                //  Get the sender socket to be used by the "server".
                _sender = _listenSocket.Accept();

                //  RtspClient default size
                byte[] buffer = new byte[8192];

                _client = new Media.Rtp.RtpClient(new Media.Common.MemorySegment(buffer, Media.Rtsp.RtspMessage.MaximumLength, buffer.Length - Media.Rtsp.RtspMessage.MaximumLength));
                _client.InterleavedData += ProcessInterleaveData;
                _client.RtpPacketReceieved += ProcessRtpPacket;

                Media.Sdp.MediaDescription md = new Media.Sdp.MediaDescription(Media.Sdp.MediaType.video, 999, "H.264", 0);

                Media.Rtp.RtpClient.TransportContext tc = new Media.Rtp.RtpClient.TransportContext(0, 1,
                    Media.RFC3550.Random32(9876), md, false, _senderSSRC);
                //  Create a Duplexed reciever using the RtspClient socket.
                tc.Initialize(_receiving);

                _client.TryAddContext(tc);
            }

            Media.Rtsp.RtspMessage lastInterleaved;

            void ProcessInterleaveData(object sender, byte[] data, int offset, int length)
            {
                ConsoleColor previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ProcessInterleaveData(): offset = " + offset + ", length = :" + length);

                byte[] buffer = new byte[length];
                Array.Copy(data, offset, buffer, 0, length);
                //  Using ASCII instead of UTF8 to get all bytes printed.
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("'" + System.Text.Encoding.ASCII.GetString(buffer) + "'");


            GetMessage:

                try
                {
                    Media.Rtsp.RtspMessage interleaved = new Media.Rtsp.RtspMessage(data, offset, length);

                    if (interleaved.MessageType == Media.Rtsp.RtspMessageType.Invalid && lastInterleaved != null)
                    {

                        interleaved.Dispose();

                        interleaved = null;

                        int lastLength = lastInterleaved.Length;

                        using (var memory = new Media.Common.MemorySegment(data, offset, length))
                        {
                            lastInterleaved.CompleteFrom(null, memory);

                            if (lastLength == lastInterleaved.Length) return;
                        }

                    }
                    else lastInterleaved = interleaved;

                    //If the last message was complete then show it in green
                    if (lastInterleaved.IsComplete) Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine("ProcessInterleaveData() RtspMessage.MessageType = " + lastInterleaved.MessageType.ToString());
                    Console.WriteLine("ProcessInterleaveData() RtspMessage.CSeq = " + lastInterleaved.CSeq);
                    Console.WriteLine("ProcessInterleaveData() RtspMessage = '" + lastInterleaved.ToString() + "'");

                    Console.ForegroundColor = previousForegroundColor;

                    int totalLength = lastInterleaved.Length;

                    if (totalLength < length)
                    {
                        offset += totalLength;
                        length -= totalLength;
                        goto GetMessage;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ProcessInterleaveData() exception:" + ex.ToString());
                    Console.WriteLine("");
                    //throw;
                }
            }

            void ProcessRtpPacket(object sender, Media.Rtp.RtpPacket packet)
            {
                ConsoleColor previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("ProcessRtpPacket(): SequenceNumber = " + packet.SequenceNumber +
                    ", Payload.Offset = " + packet.Payload.Offset + ", Payload.Count = " + packet.Payload.Count);
                Console.ForegroundColor = previousForegroundColor;
            }

            public int Send(byte[] data)
            {
                return _sender.Send(data);
            }

            public void HaveRtpClientWorkerThreadProcessSocketData()
            {
                _client.Connect();

                System.Threading.Thread.Sleep(200);   //  To make the prompt appear after the output

                //if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine("Press any key to 'dicsonnect' RtpClient work thread and continue with next test.");
                    Console.ReadKey(true);
                }

                _client.Disconnect();
            }
        }

        private static byte[] GeneratePayload(int size)
        {
            int partNumber = 0;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (sb.Length < size)
            {
                sb.Append("EncapsulatedPacketPayloadContentPartNumber" + ++partNumber + "$");
            }

            //  Using ASCII as it is not intended to be interpreted as RTSP request/response text.
            byte[] encoded = System.Text.Encoding.ASCII.GetBytes(sb.ToString());

            byte[] buffer = new byte[size];
            Array.Copy(encoded, buffer, size);

            return buffer;
        }

        private static byte[] GenerateEncapsulatingHeader(int length)
        {
            byte[] header = new byte[4];
            header[0] = 0x24;   //  '$'
            header[1] = 0;      //  Channel 0
            Media.Common.Binary.Write16(header, 2, BitConverter.IsLittleEndian, (short)length);
            return header;
        }

        private static void Issue17245_Case1(int breakingPaketLength)
        {
            int sequenceNumber = 0x3030;   //  "00"
            string line = "Case1(): SequenceNumber = ";

            System.Console.Clear();
            Console.WriteLine("TestProcessFrameData Issue17245_Case1(): Discarding of Encapsulating Frame Header");
            Console.WriteLine("breakingPaketLength = " + breakingPaketLength);
            Console.WriteLine("Correct output is 5 rows saying 'Case1()...' and 5 rows 'ProcessRtpPacket()...'");
            Console.WriteLine("No yellow rows!!!");
            Console.WriteLine("");

            TestFramework tf = new TestFramework();

            Console.WriteLine(line + sequenceNumber);
            byte[] buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p1 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p1.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p2 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p2.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(breakingPaketLength); //  Length 1245 to 1247 looses packets and it does not recover
            Media.Rtp.RtpPacket p3 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p3.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p4 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p4.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p5 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p5.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            //  Kick of the processing eventually ending up in RtpClient.ProcessFrameData()
            tf.HaveRtpClientWorkerThreadProcessSocketData();
        }

        /// <summary>
        /// This test demonstrates the first point in issue report #17245.
        /// </summary>
        public static void Issue17245_Case1_Iteration()
        {
            //  Length 1245 to 1247 looses packets and erronyously triggers ProcessInterleaveData().
            for (int size = 1244; size <= 1248; size++)
            {
                Issue17245_Case1(size);
            }
        }

        private static void Issue17245_Case2(int breakingPaketLength)
        {
            int sequenceNumber = 0x3030;   //  "00"

            System.Console.Clear();
            Console.WriteLine("TestProcessFrameData Issue17245_Case2(): Interleaved RTSPResponse");
            Console.WriteLine("breakingPaketLength = " + breakingPaketLength);
            Console.WriteLine("Correct output is 3 rows saying 'ProcessRtpPacket()...', 1 yellow row, and finaly a single row 'ProcessRtpPacket()...':");
            Console.WriteLine("");

            TestFramework tf = new TestFramework();
            //Console.WriteLine(line + sequenceNumber);
            byte[] buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p1 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p1.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            //Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p2 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p2.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            //Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(breakingPaketLength);
            Media.Rtp.RtpPacket p3 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p3.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            Media.Rtsp.RtspMessage keepAlive = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response);
            keepAlive.StatusCode = Media.Rtsp.RtspStatusCode.OK;
            keepAlive.CSeq = 34;
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.UserAgent, "Testing $UserAgent $009\r\n$\0:\0");
            keepAlive.SetHeader("Ignore", "$UserAgent $009\r\n$\0\0\aRTSP/1.0");
            keepAlive.SetHeader("$", string.Empty);
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));
            buffer = keepAlive.Prepare().ToArray();
            tf.Send(buffer);

            //Console.WriteLine(line + sequenceNumber);
            buffer = GeneratePayload(1400);
            Media.Rtp.RtpPacket p4 = new Media.Rtp.RtpPacket(2, false, false, false, 0, 0, _senderSSRC, sequenceNumber++, _timeStamp, buffer);
            buffer = p4.Prepare().ToArray();
            tf.Send(GenerateEncapsulatingHeader(buffer.Length));
            tf.Send(buffer);

            //  Kick of the processing eventually ending up in RtpClient.ProcessFrameData()
            tf.HaveRtpClientWorkerThreadProcessSocketData();
        }

        /// <summary>
        /// This test demonstrates the first point in issue report #17245.
        /// 
        /// This test will not produce valid RtspMessage objects until the issue
        /// report #17276 also is fixed.
        /// 
        /// If the RtspMessage defect is attended to first this test can also be used to
        /// extensivelly validate the correctnes of the fixing of the constructor
        /// RtspMessage(byte[] data, int offset, int length).
        /// </summary>
        public static void Issue17245_Case2_Iteration()
        {
            //  First and last iteration is a complete response.
            //  All iterations inbetween are broken in two calls to ProcessInterleaveData().
            for (int size = 1500; size >= 0; --size)
            {
                Issue17245_Case2(size);
            }
        }

        /// <summary>
        /// This test case is added to demonstrate the risk of handing a byte array to a constructor.
        /// A ctor can only return a single object even if the byte array it is given may contain more
        /// than one object.
        /// This violates OO as the caller is burdened with having to now about what should be a feature
        /// of using the class it is calling.
        /// </summary>
        public static void BackToBackRtspMessages()
        {
            System.Console.Clear();
            Console.WriteLine("TestProcessFrameData Case3(): Two back to back RTSP Responses");
            Console.WriteLine("Correct output would be 2 'ProcessInterleaveData():...' detailing RTSP responses CSeq 34 and 35:");
            Console.WriteLine("");

            TestFramework tf = new TestFramework();

            Media.Rtsp.RtspMessage keepAlive = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response);
            keepAlive.StatusCode = Media.Rtsp.RtspStatusCode.OK;
            keepAlive.CSeq = 34;
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));
            byte[] buffer = keepAlive.Prepare().ToArray();
            tf.Send(buffer);

            keepAlive = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response);
            keepAlive.StatusCode = Media.Rtsp.RtspStatusCode.OK;
            keepAlive.CSeq = 35;
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Session, "A9B8C7D6");
            keepAlive.SetHeader(Media.Rtsp.RtspHeaders.Date, DateTime.Now.ToUniversalTime().ToString("r"));
            buffer = keepAlive.Prepare().ToArray();
            tf.Send(buffer);

            //  Kick of the processing eventually ending up in RtpClient.ProcessFrameData()
            tf.HaveRtpClientWorkerThreadProcessSocketData();
        }
    }

    #endregion
}

