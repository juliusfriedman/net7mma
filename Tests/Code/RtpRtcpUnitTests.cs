#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 11 Feb 2015    Julius Friedman juliusfriedman@gmail.com  Created.
 
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

public class RtpRtcpTests
{
    //Declare a few exceptions
    static Exception versionException = new Exception("Unable to set the version"),
    extensionException = new Exception("Incorrectly set the Extensions Bit"),
    contributingSourceException = new Exception("Incorrectly set the ContributingSource nibble"),
    inValidHeaderException = new Exception("Invalid header."),
    reportBlockException = new Exception("Incorrectly set the ReportBlock 7 bits"),
    paddingException = new Exception("Incorreclty set the Padding Bit"),
    timestampException = new Exception("Incorrect Timestamp value"),
    sequenceNumberException = new Exception("Sequence Number Incorrect"),
    markerException = new Exception("Marker is not set"),
    payloadException = new Exception("Incorreclty set PayloadType bits");

    /// <summary>
    /// A format for the output which occurs when unit testing.
    /// </summary>
    internal static string TestingFormat = "{0}:=>{1}";

    public void TestRtpPacket()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //Create a RtpPacket instance
        Media.Rtp.RtpPacket p = new Media.Rtp.RtpPacket(new Media.Rtp.RtpHeader(0, false, false), Enumerable.Empty<byte>());

        //Set a few values
        p.Timestamp = 987654321;
        p.SequenceNumber = 7;
        p.ContributingSourceCount = 7;

        System.Diagnostics.Debug.Assert(p.SequenceNumber == 7, sequenceNumberException.Message);

        if (p.Timestamp != 987654321) throw timestampException;

        System.Diagnostics.Debug.Assert(p.ContributingSourceCount == 7, contributingSourceException.Message);

        //Recreate the packet from the bytes of the result of calling the methods ToArray on the Prepare instance method.
        p = new Media.Rtp.RtpPacket(p.Prepare().ToArray(), 0);

        //Perform the same tests. (Todo condense tests into seperate functions)

        if (p.SequenceNumber != 7) throw sequenceNumberException;

        System.Diagnostics.Debug.Assert(p.SequenceNumber == 7, sequenceNumberException.Message);

        if (p.ContributingSourceCount != 7) throw contributingSourceException;

        System.Diagnostics.Debug.Assert(p.ContributingSourceCount == 7, contributingSourceException.Message);

        System.Diagnostics.Debug.Assert(p.Timestamp == 987654321, timestampException.Message);

        //Cache a bitValue
        bool bitValue = false;

        //Permute every possible bit packed value that can be valid in the first and second octet
        for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
        {
            //Make a bitValue after the 0th iteration
            if (ibitValue > 0) bitValue = Convert.ToBoolean(bitValue);

            //Show the bitValue 0 or 1
            if (ibitValue <= 1) Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

            //Permute every possible value within the 2 bit Version
            for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
            {
                //Set the version
                p.Version = VersionCounter;

                //Write the version information to the console.
                Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

                //Set the bit values in the first octet
                p.Extension = p.Padding = bitValue;

                //Check the version bits after modification
                System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                //Check the Padding bit after modification
                System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                //Check the Extension bit after modification
                System.Diagnostics.Debug.Assert(p.Extension == bitValue, extensionException.Message);

                //Permute every possible value in the 7 bit PayloadCounter
                for (int PayloadCounter = 0; PayloadCounter <= sbyte.MaxValue; ++PayloadCounter)
                {
                    //Set the 7 bit value in the second octet.
                    p.PayloadType = (byte)PayloadCounter;

                    //Write the value of the PayloadCounter to the console and the packet value to the Console.
                    Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                    Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

                    //Check the PayloadType
                    System.Diagnostics.Debug.Assert(p.PayloadType == PayloadCounter, payloadException.Message);

                    //Check the Padding bit after modification
                    System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                    //Check the Extension bit after modification
                    System.Diagnostics.Debug.Assert(p.Extension == bitValue, extensionException.Message);

                    //Permute every combination for a nybble
                    for (int ContributingSourceCounter = byte.MinValue; ContributingSourceCounter <= Media.Common.Binary.FourBitMaxValue; ++ContributingSourceCounter)
                    {
                        ///////////////Set the CC nibble in the first Octet
                        p.ContributingSourceCount = (byte)ContributingSourceCounter;
                        /////////////

                        //Identify the Contributing Source Counter and the Packet's value
                        Console.Write(string.Format(TestingFormat, "\tContributingSourceCounter", ContributingSourceCounter));
                        Console.Write(string.Format(TestingFormat, " ContributingSourceCount", p.ContributingSourceCount + "\r\n"));

                        //Check the CC nibble in the first octet.
                        System.Diagnostics.Debug.Assert(p.ContributingSourceCount == ContributingSourceCounter, contributingSourceException.Message);

                        //Ensure the Version after modification
                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                        //Check the Padding bit after modification
                        System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                        //Check the Extension bit after modification
                        System.Diagnostics.Debug.Assert(p.Extension == bitValue, extensionException.Message);

                        ///////////////Serialize the packet
                        p = new Media.Rtp.RtpPacket(p.Prepare().ToArray(), 0);
                        /////////////

                        //Ensure the Version after modification
                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                        //Check the Padding bit after modification
                        System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                        //Check the Extension bit after modification
                        System.Diagnostics.Debug.Assert(p.Extension == bitValue, extensionException.Message);

                        //Check the CC nibble in the first octet.
                        System.Diagnostics.Debug.Assert(p.ContributingSourceCount == ContributingSourceCounter, contributingSourceException.Message);

                    }
                }
            }

            Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
        }

    }

    public void TestRtpExtension()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

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
                                          0xBE, 0xE5, 0x39, 0x8D, // etc. 
                                      };

        Media.Rtp.RtpPacket testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

        if (testPacket.Extension)
        {
            using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
            {

                if (rtpExtension == null) throw new Exception("Extension is null");

                if (!rtpExtension.IsComplete) throw new Exception("Extension is not complete");

                // The extension data length is (3 words / 12 bytes) 
                // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                if (rtpExtension.Size != 16) throw new Exception("Expected ExtensionLength not found");
                else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                // Check extension values are what we expected.
                if (rtpExtension.Flags != 0xABAC) throw new Exception("Expected Extension Flags Not Found");
                else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                // Test the extension data is correct
                byte[] expected = { 0xD4, 0xBB, 0x8A, 0x43, 0xFE, 0x7A, 0xC8, 0x1E, 0x00, 0xD3, 0x00, 0x00 };
                if (!rtpExtension.Data.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
                else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
            }
        }

        byte[] output = testPacket.Prepare().ToArray();

        if (output.Length != m_SamplePacketBytes.Length) throw new Exception("Packet was not the same");

        for (int i = 0, e = testPacket.Length; i < e; ++i) if (output[i] != m_SamplePacketBytes[i]) throw new Exception("Packet was not the same");


        Console.WriteLine();

        m_SamplePacketBytes = new byte[]
                                      {
                                          //RTP Header
                                             0x90,0x1a,0x01,0x6d,0xf3,0xff,0x40,0x58,0xf0,0x00,0x9c,0x5b,
                                             //Extension FF FF, Length = 0
                                             0xff,0xff,0x00,0x00

                                            ,0x00,0x00,0x15,0x1c,0x01,0xff,0xa0,0x5a,0x13,0xd2,0xa9,0xf5,0xeb,0x49,0x52,0xdb
                                            ,0x65,0xa8,0xd8,0x40,0x36,0xa8,0x03,0x81,0x41,0xf7,0xa5,0x34,0x52,0x28,0x4f,0x7a
                                            ,0x29,0x79,0xf5,0xa4,0xa0,0x02,0x8a,0x5a,0x4a,0x00,0x28,0xed,0x45,0x14,0x0c,0x28
                                            ,0xef,0x45,0x1c,0xd0,0x20,0xa3,0x34,0x51,0x40,0xc4,0xcd,0x2d,0x14,0x7e,0x34,0x00
                                            ,0x7f,0x5a,0x4a,0x5a,0x29,0x00,0x94,0x52,0xd0,0x72,0x69,0x88,0x4e,0x68,0xa3,0x9a
                                            ,0x5a,0x43,0x12,0x8a,0x39,0xa0,0x73,0x4c,0x02,0x8a,0x5a,0x29,0x00,0x63,0x8e,0x28
                                            ,0xa5,0xcf,0x14,0x94,0x00,0x51,0x45,0x14,0x08,0x4a,0x51,0x9a,0x5c,0x52,0x00,0x68
                                            ,0x01,0x40,0xa4,0xa7,0x04,0x62,0x78,0xa9,0x52,0x06,0x6e,0xd4,0x01,0x12,0x8e,0x7d
                                            ,0x69,0x36,0x92,0x71,0x8a,0xd0,0x83,0x4f,0x91,0xfa,0x29,0x35,0x7a,0x1d,0x25,0xb3
                                            ,0xf3,0xfc,0xa3,0xde,0x9d,0x85,0x73,0x11,0x62,0x63,0x56,0x63,0xb3,0x76,0xc6,0x14
                                            ,0xd6,0xc9,0x86,0xca,0xd8,0x66,0x49,0x41,0x3e,0x82,0xaa,0x4f,0xad,0xdb,0xdb,0xf1
                                            ,0x12,0x8e,0x3b,0x9a,0x05,0x71,0xb1,0x69,0x72,0x1e,0x48,0xc0,0xf7,0xa9,0x8d,0xa5
                                            ,0xbc,0x03,0x32,0x48,0x3f,0x0a,0xc4,0xbd,0xf1,0x2c,0x8f,0x90,0x1f,0x03,0xd0,0x56
                                            ,0x35,0xce,0xaf,0x2c,0x87,0xef,0x53,0xb0,0x1d,0x54,0xd7,0xf6,0xb0,0x70,0x80,0x1f
                                            ,0x73,0x59,0xb7,0x9a,0xde,0x54,0x85,0x60,0x3e,0x95,0xcc,0x4d,0x79,0x23,0xf5,0x63
                                            ,0x55,0xda,0x56,0x27,0x92,0x69,0xf2,0xdc,0x2e,0x5f,0x97,0x54,0x91,0xdc,0xe5,0x89
                                            ,0xaa,0x57,0x77,0x4e,0xea,0x70,0x6a,0x0e,0x73,0x9a,0x8d,0xbe,0xb5,0x56,0x44,0xb6
                                            ,0x46,0x8e,0x59,0xdb,0x2d,0x9c,0x76,0xf4,0xa7,0xfe,0x95,0x1a,0x9c,0x12,0x79,0xa7
                                            ,0x82,0x4d,0x50,0x8f,0x41,0xa0,0x52,0xd2,0x71,0x59,0x1a,0x00,0x34,0xef,0x73,0x49
                                            ,0x9a,0x3e,0xa6,0x80,0x0a,0x28,0xe2,0x8e,0xf4,0x00,0x01,0x4b,0x41,0x22,0x90,0x30
                                            ,0xcf,0x51,0x45,0xc4,0x07,0x9a,0x51,0xd6,0x93,0x7a,0xff,0x00,0x78,0x7e,0x74,0x82
                                            ,0x44,0x1d,0x58,0x50,0x31,0xc3,0x39,0x39,0xa3,0x34,0x82,0x58,0xc7,0xf1,0x8f,0xce
                                            ,0x93,0xcf,0x8b,0xfb,0xe2,0x8b,0x88,0x7d,0x2e,0x0d,0x45,0xf6,0x88,0xfb,0xb8,0xfc
                                            ,0xe8,0xfb,0x54,0x3f,0xf3,0xd0,0x51,0x70,0x25,0xa2,0xa1,0xfb,0x5c,0x3d,0xdc,0x50
                                            ,0x2e,0xe1,0xcf,0xdf,0x14,0x5c,0x64,0xe0,0x51,0xc8,0xa4,0x57,0x57,0xe4,0x10,0x69
                                            ,0xc7,0x91,0x40,0x84,0xa5,0xa0,0x76,0x34,0x50,0x01,0x8c,0x9a,0x29,0x7a,0x52,0xfb
                                            ,0xd0,0x03,0x40,0xce,0x73,0x4b,0xb7,0x8a,0x5e,0x94,0x84,0xd0,0x03,0x31,0xcd,0x14
                                            ,0xe3,0x49,0x8c,0xd0,0x31,0x41,0xa7,0x0f,0x5a,0x8f,0xbd,0x3d,0x78,0x34,0x08,0x7f
                                            ,0x5e,0xb4,0x62,0x8c,0x64,0xd2,0xe3,0xd6,0x80,0x12,0x81,0x4b,0x8c,0xfb,0x51,0xc5
                                            ,0x00,0x14,0x87,0x91,0x4a,0x4d,0x1e,0xbc,0xd0,0x21,0xb9,0xcd,0x3b,0xde,0x8f,0xad
                                            ,0x28,0xa0,0x05,0xf4,0xa3,0xa9,0xa3,0x14,0x94,0x80,0x05,0x07,0xda,0x8a,0x5a,0x60
                                            ,0x27,0x7a,0x3b,0xe6,0x96,0x8e,0xb4,0x80,0x4e,0xb4,0x84,0x77,0xa5,0xe4,0x52,0x1a
                                            ,0x00,0x6e,0xda,0x8c,0x8e,0xb5,0x2d,0x30,0xe7,0xad,0x03,0x18,0x6a,0x39,0x33,0x52
                                            ,0x53,0x25,0xe9,0xd6,0x81,0x94,0xe5,0xe9,0x55,0x24,0xfb,0xc3,0xeb,0x56,0xe4,0xe9
                                            ,0x55,0x1b,0xef,0x0a,0x68,0x0b,0x30,0xf2,0xcb,0xf5,0xae,0x93,0x53,0xe3,0x4a,0x1f
                                            ,0x85,0x73,0x90,0x0f,0xde,0x2f,0xd6,0xba,0x4d,0x57,0x8d,0x2c,0x1f,0xa5,0x0f,0x63
                                            ,0x2a,0xbf,0x09,0x8d,0x6b,0x7d,0x2d,0xba,0xed,0x5c,0x11,0x5a,0xb6,0x9a,0xcb,0xa9
                                            ,0x1b,0x93,0x35,0x84,0xbe,0xb5,0x62,0x2e,0xbc,0x57,0x34,0x99,0xca,0xa7,0x25,0xb1
                                            ,0xdb,0xd8,0x6b,0xaa,0x17,0xee,0x10,0xd5,0xbb,0xa3,0x6a,0xcc,0x26,0xcc,0x8d,0x95
                                            ,0x6e,0xa2,0xbc,0xfe,0xd1,0xb6,0xe3,0x9a,0xdc,0xb2,0x9f,0x18,0xe6,0xb8,0xaa,0x26
                                            ,0x3f,0xac,0x4e,0xeb,0x53,0xd3,0x81,0x59,0x10,0x11,0x82,0xa6,0xa1,0x91,0x30,0xd8
                                            ,0x27,0x22,0xa9,0xf8,0x7e,0x7f,0x36,0xc8,0x6e,0x39,0xc1,0xa4,0xbe,0xbb,0x09,0x70
                                            ,0x57,0x3d,0x2a,0x66,0xd3,0x8a,0xee,0x74,0xd6,0xd6,0x0a,0x45,0xcf,0x2d,0x4d,0x21
                                            ,0x88,0x55,0x05,0xbd,0x1e,0xb5,0x28,0xbc,0x07,0xbd,0x61,0x76,0x71,0x68,0x5b,0xfb
                                            ,0x28,0x23,0x34,0xf8,0x20,0x11,0x12,0x7d,0x6a,0x18,0x2e,0xd4,0xf0,0xc6,0xae,0x03
                                            ,0x9e,0x6b,0xb7,0x0f,0x0a,0x73,0xdd,0xea,0x8e,0xca,0x50,0x83,0xf7,0x96,0xe4,0x77
                                            ,0x11,0xf9,0xb1,0x32,0xd6,0x4c,0x96,0x27,0xba,0x9a,0xda,0xa6,0x4b,0x22,0xc6,0x3e
                                            ,0x63,0xf8,0x56,0x98,0x9a,0x51,0x4d,0xce,0xe3,0xad,0x4a,0x32,0xd5,0xb3,0x9e,0x96
                                            ,0xcf,0xda,0xa8,0xdc,0x59,0xe3,0x27,0x15,0xd0,0x3b,0x2b,0x12,0x6a,0xad,0xc0,0x52
                                            ,0x0d,0x79,0xe9,0x9e,0x7b,0x8a,0x28,0xe9,0xb6,0xf8,0xb5,0x6e,0x3b,0xd5,0x5b,0xc4
                                            ,0xc6,0x45,0x6e,0x59,0x28,0xfb,0x23,0x1f,0x7a,0xc8,0xd4,0x78,0x26,0xbb,0xe9,0x3b
                                            ,0xa3,0xd8,0xa4,0xad,0x49,0x18,0x53,0x8e,0x6b,0x1b,0x59,0x1f,0xe8,0xad,0x5b,0x33
                                            ,0x9e,0x6b,0x1b,0x59,0x39,0xb5,0x35,0xd9,0xd0,0xc6,0xaf,0xc2,0xca,0x9e,0x1d,0x1c
                                            ,0xc9,0xf5,0xab,0x77,0x03,0x13,0xb7,0xd7,0xad,0x56,0xf0,0xef,0x59,0x3e,0xb5,0x6e
                                            ,0xe0,0x66,0x67,0xf5,0xcd,0x69,0x1d,0x8c,0xa1,0xb2,0x22,0x23,0xde,0x9b,0x4e,0xa3
                                            ,0x03,0xad,0x05,0x89,0x40,0xcf,0x7a,0x5e,0xd9,0xa4,0xef,0x40,0x06,0x38,0x34,0x63
                                            ,0x1e,0xf4,0x1e,0x07,0xd6,0x8f,0x4a,0x63,0x0f,0xeb,0x48,0x07,0xad,0x3b,0x81,0x4b
                                            ,0xd7,0x14,0x80,0x6f,0x4c,0xe6,0x93,0x8c,0x75,0xa5,0x23,0xae,0x68,0xc7,0x38,0xa6
                                            ,0x21,0x31,0xda,0x93,0x18,0xa7,0xe0,0x77,0x34,0x98,0xcd,0x00,0x34,0xf7,0xed,0x49
                                            ,0x8a,0x7e,0x33,0x48,0x45,0x00,0x34,0x77,0xa3,0x93,0x4b,0x4a,0x7d,0x28,0x01,0x33
                                            ,0x8a,0x30,0x71,0xcf,0x39,0xa2,0x97,0x1e,0xd4,0x00,0x2f,0xd2,0x9e,0x29,0x00,0xa7
                                            ,0x7b,0x66,0x81,0x09,0x8f,0x6a,0x43,0x9c,0x1e,0x29,0xe4,0x66,0x9a,0x72,0x38,0xa4
                                            ,0x52,0x2b,0xc8,0x0f,0x3d,0xaa,0xb4,0xb9,0x1f,0x4a,0xb5,0x27,0x7a,0xad,0x21,0xea
                                            ,0x2a,0x59,0x48,0xa8,0xfe,0xb5,0x03,0x70,0x6a,0xc4,0x83,0x15,0x5c,0xf5,0xa0,0xa1
                                            ,0x56,0xb6,0x3c,0x3d,0xff,0x00,0x1f,0xa9,0x58,0xe3,0xde,0xb6,0x7c,0x3d,0xff,0x00
                                            ,0x1f,0x89,0x42,0x14,0xb6,0x3b,0x1a,0x43,0x41,0x6e,0x29,0x33,0x56,0x66,0x14,0xbf
                                            ,0x53,0x4d,0xcd,0x26,0xf1,0xeb,0x40,0x0e,0x34,0xdf,0xad,0x30,0xca,0xa3,0xa9,0xa8
                                            ,0x24,0xbc,0x8d,0x7a,0x9a,0x57,0x0b,0x12,0xc9,0x55,0x49,0x1d,0xcd,0x55,0xbb,0xd5
                                            ,0x02,0xa9,0xd8,0xb9,0xfa,0xd6,0x3c,0xd7,0xf3,0x4d,0x9c,0xb6,0x07,0xa0,0xa3,0x98
                                            ,0x7c,0xac,0xda,0x9a,0xea,0x18,0xb3,0xb9,0xc7,0xe1,0x59,0xd3,0xea,0xc3,0x27,0xca
                                            ,0x4e,0x7d,0x4d,0x66,0x33,0x13,0xc9,0x24,0xd3,0x0f,0xeb,0x4b,0x99,0xb1,0xa8,0xa2
                                            ,0x69,0xee,0xe7,0x9b,0xef,0x39,0xc7,0xa0,0xaa,0xc7,0x9e,0xf4,0xa6,0x9a,0x69,0x17
                                            ,0x61,0x0d,0x25,0x2f,0xd6,0x83,0x40,0xc6,0x9a,0x28,0xeb,0x41,0x14,0x00,0x94,0x1a
                                            ,0x28,0xa0,0x04,0xf7,0xa5,0xa2,0x92,0x90,0x05,0x1d,0x28,0xa5,0xa0,0x62,0x73,0x45
                                            ,0x14,0x62,0x80,0x0a,0x3d,0x68,0xa2,0x80,0x0a,0x28,0xc5,0x2d,0x30,0x0e,0x94,0x7d
                                            ,0x69,0x69,0x29,0x00,0x94,0xb8,0xcf,0x5a,0x31,0x4b,0xed,0x40,0x08,0x0d,0x18,0xe2
                                            ,0x8a,0x28,0x10,0x9c,0xd1,0x4b,0x46,0x28,0x01,0x39,0xcd,0x14,0x51,0x8a,0x00,0x31
                                            ,0x49,0x9e,0x69,0xe4,0x52,0x00,0x73,0xcd,0x03,0x12,0x8a,0x90,0x46,0xc7,0xb5,0x4f
                                            ,0x1d,0xa3,0xb9,0xc0,0x52,0x68,0x11,0x57,0x06,0x9c,0xa8,0xc6,0xb5,0xe0,0xd1,0xe5
                                            ,0x6e,0x59,0x71,0xf5,0xab,0x8b,0x63,0x69,0x6e,0x33,0x34,0xcb,0xc7,0x61,0x4e,0xc2
                                            ,0xb9,0x84,0x96,0xce,0xc7,0xa1,0xab,0x90,0x69,0x72,0xbf,0x45,0x38,0xab,0xd2,0x6a
                                            ,0x76,0x36,0xdc,0x46,0x81,0x8f,0xa9,0xac,0xeb,0xbf,0x11,0xb6,0x08,0x56,0x0a,0x3d
                                            ,0xa9,0x0b,0x53,0x4a,0x3d,0x29,0x13,0xfd,0x6b,0xaa,0x8f,0xad,0x2b,0xcf,0xa7,0xda
                                            ,0xf7,0xde,0xc2,0xb9,0x1b,0x9d,0x71,0xdf,0x3f,0x39,0x3f,0x8d,0x66,0x4f,0xa8,0xca
                                            ,0xfd,0x0d,0x52,0xb8,0x1d,0xa5,0xc7,0x88,0xd2,0x20,0x44,0x4a,0xab,0x58,0x97,0x7e
                                            ,0x23,0x95,0xc9,0xc3,0x93,0x5c,0xdb,0xca,0xef,0xc9,0x63,0x4c,0xc9,0x34,0xf9,0x3b
                                            ,0x8a,0xe8,0xd0,0xb8,0xd5,0x66,0x94,0x9c,0xb1,0xaa,0x6f,0x71,0x23,0xe7,0x2c,0x6a
                                      };

        testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

        if (testPacket.Extension)
        {
            using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
            {

                if (rtpExtension == null) throw new Exception("Extension is null");

                if (!rtpExtension.IsComplete) throw new Exception("Extension is not complete");

                // The extension data length is (3 words / 12 bytes) 
                // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                if (rtpExtension.Size != 4) throw new Exception("Expected ExtensionLength not found");
                else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                // Check extension values are what we expected.
                if (rtpExtension.Flags != 0xFFFF) throw new Exception("Expected Extension Flags Not Found");
                else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                // Test the extension data is correct
                byte[] expected = Media.Common.MemorySegment.EmptyBytes;
                if (!rtpExtension.Data.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
                else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
            }
        }

    }

    public void TestRtcpPacket()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //Write all Abstrractions to the console
        foreach (var abstraction in Media.Rtcp.RtcpPacket.GetImplementedAbstractions())
            Console.WriteLine(string.Format(TestingFormat, "\tFound Abstraction", "Implemented By" + abstraction.Name));

        //Write all Implementations to the console
        foreach (var implementation in Media.Rtcp.RtcpPacket.GetImplementations())
            Console.WriteLine(string.Format(TestingFormat, "\tPayloadType " + implementation.Key, "Implemented By" + implementation.Value.Name));

        //Create a RtpPacket instance
        Media.Rtcp.RtcpPacket p = new Media.Rtcp.RtcpPacket(new Media.Rtcp.RtcpHeader(0, 0, false, 0), Enumerable.Empty<byte>());

        //Check the Padding bit after modification
        System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == 0, "SynchronizationSourceIdentifier should equal 0");

        //Set a values
        p.SynchronizationSourceIdentifier = 7;

        System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == 7, "SynchronizationSourceIdentifier should equal 7");

        //Cache a bitValue
        bool bitValue = false;

        //Test every possible bit packed value that can be valid in the first and second octet
        for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
        {
            //Make a bitValue after the 0th iteration
            if (ibitValue > 0) bitValue = Convert.ToBoolean(ibitValue);

            //Complete tested the first and second octets with the current bitValue
            Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

            //Permute every possible value within the 2 bit Version
            for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
            {
                //Set the version
                p.Version = VersionCounter;

                //Write the version information to the console.
                Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

                //Set the bit values in the first octet
                p.Padding = bitValue;

                //Check the version bits after modification
                System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                //Check the Padding bit after modification
                System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                //Permute every possible value in the 7 bit PayloadCounter
                for (int PayloadCounter = 0; PayloadCounter <= byte.MaxValue; ++PayloadCounter)
                {
                    //Set the 7 bit value in the second octet.
                    p.PayloadType = (byte)PayloadCounter;

                    //Write the value of the PayloadCounter to the console and the packet value to the Console.
                    Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                    Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

                    //Check the PayloadType
                    System.Diagnostics.Debug.Assert(p.PayloadType == PayloadCounter, payloadException.Message);

                    //Check the Padding bit after setting the PayloadType
                    System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                    //Permute every combination for a nybble
                    for (int ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
                    {
                        ///////////////Set the CC nibble in the first Octet
                        p.BlockCount = (byte)ReportBlockCounter;
                        /////////////                            

                        //Identify the Contributing Source Counter and the Packet's value
                        Console.Write(string.Format(TestingFormat, "\tReportBlockCounter", ReportBlockCounter));
                        Console.Write(string.Format(TestingFormat, " BlockCount", p.BlockCount + "\r\n"));

                        //Check the BlockCount
                        System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, reportBlockException.Message);

                        //Ensure the Version after modification
                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                        //Check the Padding after modification
                        System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                        ///////////////Serialize the packet
                        p = new Media.Rtcp.RtcpPacket(p.Prepare().ToArray(), 0);
                        /////////////

                        //Ensure the version remains after modification
                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                        //Ensure the Padding bit after modification
                        System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                        //Check the BlockCount after modification
                        System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, reportBlockException.Message);

                        //Check for a valid header
                        if (false == p.Header.IsValid(VersionCounter, PayloadCounter, bitValue)
                            || //Check for validation per RFC3550 A.1 when the test permits
                            false == bitValue &&
                            VersionCounter > 1 &&
                            PayloadCounter >= 200 &&
                            PayloadCounter <= 201 &&
                            false == Media.RFC3550.IsValidRtcpHeader(p.Header, VersionCounter)) throw inValidHeaderException;

                        //Perform checks with length in words set incorrectly
                    }
                }
            }
            Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
        }
    }

    public void TestRtcpPacketExamples()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        byte[] output;

        //Keep a copy of these exceptions to throw in case some error occurs.
        Exception invalidLength = new Exception("Invalid Length"), invalidData = new Exception("Invalid Data in packet"), invalidPadding = new Exception("Invalid Padding"), incompleteFalse = new Exception("Packet IsComplete is false");

        //Create a Media.RtcpPacket with only a header (results in 8 octets of 0x00 which make up the header)
        Media.Rtcp.RtcpPacket rtcpPacket = new Media.Rtcp.RtcpPacket(0, 0, 0, 0, 0, 0);

        //Prepare a sequence which contains the data in the packet including the header
        IEnumerable<byte> preparedPacket = rtcpPacket.Prepare();

        //Check for an invlaid length
        if (rtcpPacket.Payload.Count > 0 || rtcpPacket.Header.LengthInWordsMinusOne != 0 && rtcpPacket.Length != Media.Rtcp.RtcpHeader.Length || preparedPacket.Count() != Media.Rtcp.RtcpHeader.Length) throw invalidLength;

        //Check for any data in the packet binary
        if (preparedPacket.Any(o => o != default(byte))) throw invalidData;

        //Set padding in the header
        rtcpPacket.Padding = true;

        //Check for some invalid valid
        if (rtcpPacket.PaddingOctets > 0) throw invalidPadding;

        //Ensure the packet is complete
        if (rtcpPacket.IsComplete == false) throw incompleteFalse;

        //Add nothing to the payload
        rtcpPacket.AddBytesToPayload(Media.RFC3550.CreatePadding(0), 0, 0);

        //Ensure the packet is complete
        if (rtcpPacket.IsComplete == false) throw incompleteFalse;

        //Check for some invalid value
        if (rtcpPacket.PaddingOctets > 0) throw invalidPadding;

        //Make a bunch of packets with padding
        for (int paddingAmount = 1, e = byte.MaxValue; paddingAmount <= e; ++paddingAmount)
        {

            //Write information for the test to the console
            Console.WriteLine(string.Format(TestingFormat, "Making Media.RtcpPacket with Padding", paddingAmount));

            //Try to make a padded packet with the given amount
            rtcpPacket = new Media.Rtcp.RtcpPacket(0, 0, paddingAmount, 0, 0, 0);

            //A a 4 bytes which are not padding related
            rtcpPacket.AddBytesToPayload(Enumerable.Repeat(default(byte), 4), 0, 1);

            //Check ReadPadding works after adding bytes to the payload
            if (rtcpPacket.PaddingOctets != paddingAmount) throw invalidPadding;

            //Ensure the packet is complete
            if (rtcpPacket.IsComplete == false) throw incompleteFalse;

            //Write information for the test to the console
            Console.WriteLine(string.Format(TestingFormat, "Packet Length", rtcpPacket.Length));

            //Write information for the test to the console
            Console.WriteLine(string.Format(TestingFormat, "Packet Padding", rtcpPacket.PaddingOctets));
        }

        //Create a new SendersReport with no blocks
        using (Media.Rtcp.RtcpReport testReport = new Media.Rtcp.SendersReport(2, false, 0, 7))
        {
            //The Media.RtcpData property contains all data which in the Media.RtcpPacket without padding
            if (testReport.RtcpData.Count() != 20 && testReport.Length != 20) throw invalidLength;

            output = testReport.Prepare().ToArray();//should be exactly equal to example
        }

        //Example of a Senders Report
        byte[] example = new byte[]
                         {
                            0x81,0xc8,0x00,0x0c,0xa3,0x36,0x84,0x36,0xd4,0xa6,0xaf,0x65,0x00,0x00,0x00,0x0,
                            0xcb,0xf9,0x44,0xd0,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xa3,0x36,0x84,0x36,
                            0x00,0xff,0xff,0xff,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                            0x00,0x00,0x00,0x00
                         };

        rtcpPacket = new Media.Rtcp.RtcpPacket(example, 0);
        if (rtcpPacket.Length != example.Length) throw new Exception("Invalid Length.");

        //Make a SendersReport to access the SendersInformation and ReportBlocks, do not dispose the packet when done with the report
        using (Media.Rtcp.SendersReport sr = new Media.Rtcp.SendersReport(rtcpPacket, false))
        {
            //Check the invalid block count
            if (sr.BlockCount != 1) throw new Exception("Invalid Block Count!");
            else Console.WriteLine(sr.BlockCount);//16, should be 1

            if ((uint)sr.SynchronizationSourceIdentifier != (uint)2738258998) throw new Exception("Invalid Senders SSRC!");
            else Console.WriteLine(sr.SynchronizationSourceIdentifier);//0xa3368436

            //Ensure setting the value through a setter is correct
            sr.NtpTimestamp = sr.NtpTimestamp;//14697854519044210688
            if ((ulong)sr.NtpTimestamp != 3567693669) throw new Exception("Invalid NtpTimestamp!");
            else Console.WriteLine(sr.NtpTimestamp);

            //Timestamp
            if ((uint)sr.RtpTimestamp != 3422110928) throw new Exception("Invalid RtpTimestamp!");
            else Console.WriteLine(sr.RtpTimestamp);//0

            //Data in report (Should only be 1)
            foreach (Media.Rtcp.IReportBlock rb in sr)
            {
                if ((uint)rb.BlockIdentifier != 2738258998) throw new Exception("Invalid Source SSRC");
                else if (rb is Media.Rtcp.ReportBlock)
                {
                    Media.Rtcp.ReportBlock asReportBlock = (Media.Rtcp.ReportBlock)rb;

                    Console.WriteLine(asReportBlock.SendersSynchronizationSourceIdentifier);//0
                    Console.WriteLine(asReportBlock.FractionsLost);//0
                    Console.WriteLine(asReportBlock.CumulativePacketsLost);//0
                    Console.WriteLine(asReportBlock.ExtendedHighestSequenceNumberReceived);//0
                    Console.WriteLine(asReportBlock.InterarrivalJitterEstimate);//0
                    Console.WriteLine(asReportBlock.LastSendersReportTimestamp);//0
                }
            }

            //Check the length to be exactly the same as the example 
            if (sr.Length != example.Length) throw new Exception("Invalid Length");

            //Verify SendersReport byte for byte
            output = sr.Prepare().ToArray();//should be exactly equal to example
            for (int i = 0, e = example.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");
        }

        if (rtcpPacket.Header.IsDisposed || rtcpPacket.IsDisposed) throw new Exception("Disposed the Media.RtcpPacket");

        //Now the packet can be disposed
        rtcpPacket.Dispose();
        rtcpPacket = null;

        rtcpPacket = new Media.Rtcp.SendersReport(2, false, 0, 7);
        example = rtcpPacket.Prepare().ToArray();

        foreach (Media.Rtcp.IReportBlock rb in rtcpPacket as Media.Rtcp.RtcpReport)
            Console.WriteLine(rb);

        //Next Sub Test

        //Create a GoodbyeReport with no block count
        using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7))
        {
            output = testReport.Prepare().ToArray();

            if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != 1 || testReport.Length != 8) throw new Exception("Invalid Length");

            if (testReport.BlockCount != 0) throw reportBlockException;

            if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");
        }



        //Add a Reason For Leaving

        using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7, System.Text.Encoding.ASCII.GetBytes("v")))
        {
            output = testReport.Prepare().ToArray();

            if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != 2 || testReport.Length != 12) throw new Exception("Invalid Length");

            if (testReport.BlockCount != 0) throw reportBlockException;

            if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");
        }

        //Next Sub Test
        /////

        //Recievers Report and Source Description
        example = new byte[] { 0x81,0xc9,0x00,0x07,
                                   0x69,0xf2,0x79,0x50,
                                   0x61,0x37,0x94,0x50,
                                   0xff,0xff,0xff,0xff,
                                   0x00,0x01,0x00,0x52,
                                   0x00,0x00,0x0e,0xbb,
                                   0xce,0xd4,0xc8,0xf5,
                                   0x00,0x00,0x84,0x28,
                                   
                                   0x81,0xca,0x00,0x04,
                                   0x69,0xf2,0x79,0x50,
                                   0x01,0x06,0x4a,0x61,
                                   0x79,0x2d,0x50,0x43,
                                   0x00,0x00,0x00,0x00
            };


        //Could check for multiple packets with a function without having to keep track of the offset with the Media.RtcpPacket.GetPackets Function
        Media.Rtcp.RtcpPacket[] foundPackets = Media.Rtcp.RtcpPacket.GetPackets(example, 0, example.Length).ToArray();
        Console.WriteLine(foundPackets.Length);

        //Or manually for some reason
        rtcpPacket = new Media.Rtcp.RtcpPacket(example, 0); // The same as foundPackets[0]
        using (Media.Rtcp.ReceiversReport rr = new Media.Rtcp.ReceiversReport(rtcpPacket, false))
        {
            Console.WriteLine(rr.SynchronizationSourceIdentifier);//1777498448

            //Check the invalid block count
            if (rr.BlockCount != 1) throw new Exception("Invalid Block Count!");
            else Console.WriteLine(rr.BlockCount);//16, should be 1

            using (var enumerator = rr.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Console.WriteLine("Current IReportBlock Identifier: " + enumerator.Current.BlockIdentifier);//1631032400

                    //If the instance boxed in the Interface is a ReportBlock
                    if (enumerator.Current is Media.Rtcp.ReportBlock)
                    {
                        //Unbox the Interface as it's ReportBlock Instance
                        Media.Rtcp.ReportBlock asReportBlock = enumerator.Current as Media.Rtcp.ReportBlock;

                        Console.WriteLine("Found a ReportBlock");

                        //Print the instance information
                        Console.WriteLine("FractionsLost: " + asReportBlock.FractionsLost);//255/256 0xff
                        Console.WriteLine("CumulativePacketsLost: " + asReportBlock.CumulativePacketsLost);//-1, 0xff,0xff,0xff
                        Console.WriteLine("ExtendedHighestSequenceNumberReceived: " + asReportBlock.ExtendedHighestSequenceNumberReceived);//65618, 00, 01, 00, 52
                        Console.WriteLine("InterarrivalJitterEstimate: " + asReportBlock.InterarrivalJitterEstimate);//3771
                        Console.WriteLine("LastSendersReportTimestamp: " + asReportBlock.LastSendersReportTimestamp);//3470000128
                    }
                    else //Not a ReportBlock
                    {
                        Console.WriteLine("Current IReportBlock TypeName: " + enumerator.Current.GetType().Name);
                        Console.WriteLine("Current IReportBlock Data: " + BitConverter.ToString(enumerator.Current.BlockData.ToArray()));
                    }
                }
            }

            //Verify RecieversReport byte for byte
            output = rr.Prepare().ToArray();//should be exactly equal to example
            for (int i = 0, e = rr.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

        }

        if (rtcpPacket.Header.IsDisposed || rtcpPacket.IsDisposed) throw new Exception("Disposed the Media.RtcpPacket");

        //Now the packet can be disposed
        rtcpPacket.Dispose();
        rtcpPacket = null;

        //Make another packet instance from the rest of the example data.
        rtcpPacket = new Media.Rtcp.RtcpPacket(example, output.Length);

        //Create a SourceDescriptionReport from the packet instance to access the SourceDescriptionChunks
        using (Media.Rtcp.SourceDescriptionReport sourceDescription = new Media.Rtcp.SourceDescriptionReport(rtcpPacket, false))
        {

            foreach (var chunk in sourceDescription.GetChunkIterator())
            {
                Console.WriteLine(string.Format(TestingFormat, "Chunk Identifier", chunk.ChunkIdentifer));
                //Use a SourceDescriptionItemList to access the items within the Chunk
                //This is performed auto magically when using the foreach pattern
                foreach (Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem item in chunk /*.AsEnumerable<Rtcp.SourceDescriptionItem>()*/)
                {
                    Console.WriteLine(string.Format(TestingFormat, "Item Type", item.ItemType));
                    Console.WriteLine(string.Format(TestingFormat, "Item Length", item.Length));
                    Console.WriteLine(string.Format(TestingFormat, "Item Data", BitConverter.ToString(item.Data.ToArray())));
                }
            }

            //Verify SourceDescriptionReport byte for byte
            output = sourceDescription.Prepare().ToArray();//should be exactly equal to example
            for (int i = output.Length, e = sourceDescription.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

        }

        //ApplicationSpecific - qtsi

        example = new byte[] { 0x81, 0xcc, 0x00, 0x06, 0x4e, 0xc8, 0x79, 0x50, 0x71, 0x74, 0x73, 0x69, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x61, 0x74, 0x00, 0x04, 0x00, 0x00, 0x00, 0x14 };

        rtcpPacket = new Media.Rtcp.RtcpPacket(example, 0);

        //Make a ApplicationSpecificReport instance
        Media.Rtcp.ApplicationSpecificReport app = new Media.Rtcp.ApplicationSpecificReport(rtcpPacket);

        //Check the name to be equal to qtsi
        if (!app.Name.SequenceEqual(System.Text.Encoding.UTF8.GetBytes("qtsi"))) throw new Exception("Invalid App Packet Type");

        //Check the length
        if (rtcpPacket.Length != example.Length) throw new Exception("Invalid Legnth");

        //Verify ApplicationSpecificReport byte for byte
        output = rtcpPacket.Prepare().ToArray();//should be exactly equal to example
        for (int i = 0, e = example.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

        //Test making a packet with a known length in bytes
        Media.Rtcp.SourceDescriptionReport sd = new Media.Rtcp.SourceDescriptionReport(2);
        byte[] sdOut = sd.Prepare().ToArray();

        //1 word when the ssrc is present but would be an invalid sdes because blockCount = 0
        if (!sd.IsComplete || sd.Length != Media.Rtcp.RtcpHeader.Length || sd.Header.LengthInWordsMinusOne != ushort.MaxValue) throw new Exception("Invalid Length");

        sd = new Media.Rtcp.SourceDescriptionReport(2);
        byte[] itemData = System.Text.Encoding.UTF8.GetBytes("FLABIA-PC");
        sd.Add((Media.Rtcp.IReportBlock)new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk((int)0x1AB7C080, new Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem(Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName, itemData.Length, itemData, 0))); // SSRC(4) ItemType(1), Length(1), ItemValue(9) = 15 Bytes
        rtcpPacket = sd; // Header = 4 Bytes in a SourceDescription, The First Chunk is `Overlapped` in the header.
        //asPacket now contains 11 octets in the payload.
        //asPacket now has 1 block (1 chunk of 15 bytes)
        //asPacket is 19 octets long, 11 octets in the payload and 8 octets in the header
        //asPacket would have a LengthInWordsMinusOne of 3 because 19 / 4 = 4 - 1 = 3
        //But null octets are added (Per RFC3550 @ Page 45 [Paragraph 2] / http://tools.ietf.org/html/rfc3550#appendix-A.4)
        //19 + 1 = 20, 20 / 4 = 5 - 1 = 4.
        if (!rtcpPacket.IsComplete || rtcpPacket.Length != 20 || rtcpPacket.Header.LengthInWordsMinusOne != 4) throw new Exception("Invalid Length");
    }
}