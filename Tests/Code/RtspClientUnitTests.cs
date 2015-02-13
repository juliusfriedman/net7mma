#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 12 Feb 2015    Julius Friedman juliusfriedman@gmail.com Created.
 
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
using System.Collections.Generic;
using System.Linq;


public class RtspClientUnitTests
{
    public static void TestRtspInterleavedFraming()
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
                Console.WriteLine("\tInterleaved (@" + offset + ", count=" + count + ") =>" + System.Text.Encoding.ASCII.GetString(data, offset, count));

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

                    System.Diagnostics.Debug.Assert(foundLen <= max);

                    if (foundLen > 0)
                    {
                        //Move the offset
                        offset += foundLen;

                        max -= foundLen;

                        remains -= foundLen;
                    }
                    else
                    {
                        ++offset;

                        --max;

                        --remains;
                    }
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
}