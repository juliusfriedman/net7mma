#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 16 Dec 2014    `exterminator`      Created @ http://net7mma.codeplex.com/workitem/17245
 28 Dec 2014    Julius Friedman-juliusfriedman@gmail.com  modified to ensure the Payload type of the generated packets was the same as indicated in the MediaDescription and changed the range of the Issue2 to include more bytes.
 2 Feb 2014     Julius Friedman Added TestInterleavedFraming    

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

namespace Media.UnitTests
{

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
                    tc.Initialize(_receiving, _receiving);

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
                    _client.Activate();

                    //while (false == _client.IsConnected) System.Threading.Thread.Sleep(0);   //  To make the prompt appear after the output


                    //if (System.Diagnostics.Debugger.IsAttached)
                    {
                        Console.WriteLine("Press any key to 'dicsonnect' RtpClient work thread and continue with next test. or Q to Quit");
                        if (Console.ReadKey(true).Key == ConsoleKey.Q) quit = true;
                    }

                    _client.Disconnect();
                }
            }

            private static byte[] GeneratePayload(int size)
            {
                if (size > ushort.MaxValue) return Media.Common.MemorySegment.EmptyBytes;

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
                for (size = 1244; size <= 1248; size++)
                {
                    Console.WriteLine("size=" + size + " Started");

                    Issue17245_Case1(size);

                    Console.WriteLine("size=" + size + ", Done!");

                    if (quit) break;
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

            static int size = 0;

            static bool quit = false;

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
                for (size = 1500; size >= 0; --size)
                {
                    Console.WriteLine("size=" + size + " Started");

                    Issue17245_Case2(size);

                    Console.WriteLine("size=" + size + " Done.");

                    if (quit) break;
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

        internal static void TestInterleavedFraming()
        {

            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            //Create a rtp client to mock the test
            using (Media.Rtp.RtpClient test = new Media.Rtp.RtpClient())
            {

                Media.Rtsp.RtspMessage lastInterleaved = null;

                int rtspOut = 0, rtspIn = 0;

                //Setup an even to see what data was transmited.
                test.InterleavedData += (sender, data, offset, count) =>
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    //Console.WriteLine("\tInterleaved (@" + offset + ", count=" + count + ") =>" + System.Text.Encoding.ASCII.GetString(data, offset, count));

                GetMessage:
                    Media.Rtsp.RtspMessage interleaved = new Media.Rtsp.RtspMessage(data, offset, count);

                    if (interleaved.MessageType == Media.Rtsp.RtspMessageType.Invalid && lastInterleaved != null)
                    {

                        interleaved.Dispose();

                        interleaved = null;

                        int lastLength = lastInterleaved.Length;

                        using (var memory = new Media.Common.MemorySegment(data, offset, count))
                        {
                            int used = lastInterleaved.CompleteFrom(null, memory);

                            if (used == 0 || lastLength == lastInterleaved.Length) return;

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Added Data (" + used + ") Bytes");
                        }
                    }
                    else
                    {
                        lastInterleaved = interleaved;
                        ++rtspIn;
                    }

                    if (lastInterleaved.IsComplete)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;

                        Console.WriteLine("Complete Message: " + lastInterleaved);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                        Console.WriteLine("Incomplete Message: " + lastInterleaved);
                    }

                    int totalLength = lastInterleaved.Length;

                    if (totalLength < count)
                    {
                        offset += totalLength;
                        count -= totalLength;
                        goto GetMessage;
                    }

                };

                //Loop 255 times
                foreach (var testIndex in Enumerable.Range(byte.MinValue, byte.MaxValue))
                {

                    //reset rtsp count
                    rtspOut = rtspIn = 0;

                    //Declare a channel randomly
                    int channel = Media.Utility.Random.Next(byte.MinValue, byte.MaxValue);

                    //Declare a random length
                    int length = Media.Utility.Random.Next(ushort.MinValue, ushort.MaxValue);

                    //Determine an actual length
                    int actualLength = 0;

                    //Make a header
                    var header = new byte[] { Media.Rtp.RtpClient.BigEndianFrameControl, (byte)channel, 0, 0 };

                    //Write the length in the header
                    Media.Common.Binary.Write16(header, 2, BitConverter.IsLittleEndian, (short)length);

                    //Get the data indicated
                    //var data = header.Concat(Enumerable.Repeat(default(byte), actualLength));

                    List<byte> allData = new List<byte>();

                    //Add more data until actualLength is less then length
                    while (actualLength < length)
                    {
                        //Make a message 
                        var rtspBytes = new Media.Rtsp.RtspMessage(Media.Rtsp.RtspMessageType.Response, 1.0, Media.Rtsp.RtspMessage.DefaultEncoding)
                        {
                            StatusCode = Media.Rtsp.RtspStatusCode.OK,
                            CSeq = Media.Utility.Random.Next(byte.MinValue, int.MaxValue),
                            UserAgent = "$UserAgent $007\r\n$\0\0\aRTSP/1.0",
                            Body = "$00Q\r\n$\0:\0"
                        }.Prepare();

                        byte[] data = new byte[Media.Utility.Random.Next(1, length)];

                        Media.Utility.Random.NextBytes(data);

                        //determine if there is more space
                        actualLength += data.Length;

                        //Put a message here
                        allData.AddRange(rtspBytes.ToArray());

                        //Increment the outgoing message count
                        ++rtspOut;

                        //Put some data
                        allData.AddRange(data);

                        //Put a message
                        allData.AddRange(rtspBytes.ToArray());

                        //Increment the outgoing message count
                        ++rtspOut;

                        //If there is more space put another binary frame
                        if (actualLength < length)
                        {
                            int needed = length - actualLength;

                            //Randomize needed
                            needed = Media.Utility.Random.Next(0, needed);

                            var headerLast = new byte[] { 0x24, (byte)channel, 0, 0 };

                            Media.Common.Binary.Write16(headerLast, 2, BitConverter.IsLittleEndian, (short)needed);

                            data = new byte[needed];

                            Media.Utility.Random.NextBytes(data);

                            allData.AddRange(data);

                            actualLength += needed;
                        }
                    }

                    //Start at 0
                    int offset = 0;

                    byte[] buffer = allData.ToArray();

                    int max = buffer.Length, remains = actualLength;

                    //Enumerate the buffer looking for data to parse

                    //TODO Improve test by setting expections of packets to receive

                    while (remains > 0)
                    {
                        //Parse the data "received" which should always be 4 bytes longer then what was actually present.
                        int foundLen = test.ProcessFrameData(buffer, offset, remains, null);

                        Console.WriteLine("Indicated: " + actualLength + " Actual: " + max + " Found: " + foundLen);

                        if (foundLen > max) throw new Exception("TestInterleavedFraming found an invalid length.");

                        //Move the offset
                        offset += foundLen;

                        max -= foundLen;

                        remains -= foundLen;
                    }

                    //Some Rtsp messages may have been hidden by invalid tcp frames which indicated a longer length then they actually had.
                    if (rtspOut > rtspIn)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Missed:" + (rtspOut - rtspIn) + " Messages of" + rtspOut);
                    }
                    else if (rtspIn == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Missed All Messages");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Missed No Messages");
                    }
                }
            }
        }

        public void TestNull()
        {
            //base class?
            //UnitTestBase
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            Media.Rtp.RtpClient rtpClient = default(Media.Rtp.RtpClient);

            try { rtpClient = Media.Rtp.RtpClient.FromSessionDescription(default(Media.Sdp.SessionDescription)); }
            catch { }

            System.Diagnostics.Debug.Assert(rtpClient == null, "Must not have created a RtpClient from a null SessionDescription");


            //Create a client
            using (rtpClient = new Media.Rtp.RtpClient())
            {
                //Attempt to find a context
                Media.Rtp.RtpClient.TransportContext contextAvailable = rtpClient.GetTransportContexts().FirstOrDefault();

                System.Diagnostics.Debug.Assert(contextAvailable == null, "Found a Context when there was no Session or Media Description");

                using (contextAvailable = new Media.Rtp.RtpClient.TransportContext(0, 1, 0))
                {
                    //Usually indicated by the profile
                    bool padding = false, extension = false;

                    //Create a RtpPacket from the 
                    using (var rtpPacket = new Media.Rtp.RtpPacket(contextAvailable.Version, padding, extension, null))
                    {
                        System.Diagnostics.Debug.Assert(rtpClient.SendRtpPacket(rtpPacket) == 0, "Sent a packet when there was no Session or Media Description or TransportContext");
                    }
                }
            }
        }

        public void TestCustom()
        {

            byte[] random = new byte[5];

            Media.Utility.Random.NextBytes(random);

            // Create SDP offer (Step 1).
            string originatorAndSession = String.Format("{0} {1} {2} {3} {4} {5}", "-", BitConverter.ToString(random).Replace("-", string.Empty), "0", "IN", "IP4", "10.1.1.2");
            using (var sdp = new Media.Sdp.SessionDescription(0, originatorAndSession, "sipsorcery"))
            {
                sdp.Add(new Media.Sdp.SessionDescriptionLine("c=IN IP4 10.1.1.2"), false);
                var audioAnnouncement = new Media.Sdp.MediaDescription(Media.Sdp.MediaType.audio, 0, "SDP_TRANSPORT", 0);
                sdp.Add(audioAnnouncement, false);

                // Set up the RTP channel (Step 2).
                using (var _rtpAudioClient = Media.Rtp.RtpClient.FromSessionDescription(sdp))
                {
                    var _audioRTPTransportContext = _rtpAudioClient.GetTransportContexts().FirstOrDefault(); // The tranpsort context is null at this point.

                    System.Diagnostics.Debug.Assert(_audioRTPTransportContext != null, "Cannot find the context");

                    System.Diagnostics.Debug.Assert(_audioRTPTransportContext.IsConnected == false, "Found a connected context");

                    _rtpAudioClient.Activate();

                    System.Diagnostics.Debug.Assert(_audioRTPTransportContext.IsConnected == true, "Found a disconnected context");
                }
            }
        }
    }

}