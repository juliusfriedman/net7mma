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

    //Todo Seperate

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
            //if (ibitValue <= 1) Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

            //Permute every possible value within the 2 bit Version
            for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
            {
                //Set the version
                p.Version = VersionCounter;

                //Write the version information to the console.
                //Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                //Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

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
                    //Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                    //Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

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
                        //Console.Write(string.Format(TestingFormat, "\tContributingSourceCounter", ContributingSourceCounter));
                        //Console.Write(string.Format(TestingFormat, " ContributingSourceCount", p.ContributingSourceCount + "\r\n"));

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

            //Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
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
            //Console.WriteLine(string.Format(TestingFormat, "\tbitValue", bitValue + "\r\n"));

            //Permute every possible value within the 2 bit Version
            for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
            {
                //Set the version
                p.Version = VersionCounter;

                //Write the version information to the console.
                //Console.Write(string.Format(TestingFormat, "\tVersionCounter", VersionCounter));
                //Console.Write(string.Format(TestingFormat, " Version", p.Version + "\r\n"));

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
                    //Console.Write(string.Format(TestingFormat, "\tPayloadCounter", PayloadCounter));
                    //Console.Write(string.Format(TestingFormat, " PayloadType", p.PayloadType + "\r\n"));

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
                        //Console.Write(string.Format(TestingFormat, "\tReportBlockCounter", ReportBlockCounter));
                        //Console.Write(string.Format(TestingFormat, " BlockCount", p.BlockCount + "\r\n"));

                        //Check the BlockCount
                        System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, reportBlockException.Message);

                        //Ensure the Version after modification
                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, versionException.Message);

                        //Check the Padding after modification
                        System.Diagnostics.Debug.Assert(p.Padding == bitValue, paddingException.Message);

                        ///////////////Serialize the packet
                        using (p = new Media.Rtcp.RtcpPacket(p.Prepare().ToArray(), 0))
                        {
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
                        }

                        //Perform checks with length in words set incorrectly
                    }
                }
            }
            
            //Console.WriteLine(string.Format(TestingFormat, "\t*****Completed an iteration wih bitValue", bitValue + "*****"));
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
            rtcpPacket.AddBytesToPayload(Enumerable.Repeat(default(byte), 4));

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
        using (Media.Rtcp.RtcpReport testReport = new Media.Rtcp.SendersReport(2, 0, 7))
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
                //if ((uint)rb.BlockIdentifier != 2738258998) throw new Exception("Invalid BlockIdentifier");
                //else if (rb is Media.Rtcp.ReportBlock)
                //{
                Media.Rtcp.ReportBlock asReportBlock = (Media.Rtcp.ReportBlock)rb;

                if (rb.BlockIdentifier != asReportBlock.SendersSynchronizationSourceIdentifier) throw new Exception("Invalid SendersSynchronizationSourceIdentifier");

                Console.WriteLine(asReportBlock.SendersSynchronizationSourceIdentifier);//0
                Console.WriteLine(asReportBlock.FractionsLost);//0
                Console.WriteLine(asReportBlock.CumulativePacketsLost);//0
                Console.WriteLine(asReportBlock.ExtendedHighestSequenceNumberReceived);//0
                Console.WriteLine(asReportBlock.InterarrivalJitterEstimate);//0
                Console.WriteLine(asReportBlock.LastSendersReportTimestamp);//0
                //}


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

        rtcpPacket = new Media.Rtcp.SendersReport(2, 0, 7);

        example = rtcpPacket.Prepare().ToArray();

        if (rtcpPacket.SynchronizationSourceIdentifier != 7) throw new Exception("Unexpected SynchronizationSourceIdentifier");

        if (rtcpPacket.BlockCount != 0) throw new Exception("Unexpected BlockCount");

        //Check the Length, 8 Byte Header, 20 Byte SendersInformation
        if (rtcpPacket.Length != Media.Rtcp.RtcpHeader.Length + Media.Rtcp.ReportBlock.ReportBlockSize) throw new Exception("Unexpected BlockCount");

        //Iterate each IReportBlock in the RtcpReport representation of the rtcpPacket instance
        foreach (Media.Rtcp.IReportBlock rb in rtcpPacket as Media.Rtcp.RtcpReport)
        {
            Console.WriteLine(rb);

            throw new Exception("Unexpected BlockCount");
        }

        //Next Sub Test

        //Create a GoodbyeReport with no SourceList, e.g. a BlockCount of 0.
        //There should be 8 bytes, 4 for the RtcpHeader and 4 for the SynchronizationSourceIdentifier
        //The LengthInWordsMinusOne should equal 1 (1 + 1 = 2, 2 * 4 = 8)
        using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7))
        {
            output = testReport.Prepare().ToArray();

            if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != 1 || testReport.Length != 8) throw new Exception("Invalid Length");

            if (testReport.BlockCount != 0) throw reportBlockException;

            if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");
        }

        //Add a Reason For Leaving

        //Should now have 4 words... Header, SSRC, Block, Reason
        using (var testReport = new Media.Rtcp.GoodbyeReport(2, 7, System.Text.Encoding.ASCII.GetBytes("v")))
        {
            output = testReport.Prepare().ToArray();

            //3
            if (output.Length != testReport.Length || testReport.Header.LengthInWordsMinusOne != 2 || testReport.Length != 12) throw new Exception("Invalid Length");

            if (testReport.BlockCount != 0) throw reportBlockException;

            if (output[7] != 7 || testReport.SynchronizationSourceIdentifier != 7) throw new Exception("Invalid ssrc");

            if (false == testReport.HasReasonForLeaving) throw new Exception("Has no reason for leaving.");

            if (System.Text.Encoding.ASCII.GetString(testReport.ReasonForLeaving.ToArray()) != "v") throw new Exception("Does not have expected reason for leaving.");
        }

        //Next Sub Test
        /////

        //Recievers Report and Source Description
        example = new byte[] {   0x81,0xc9,0x00,0x07,
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


        int foundPackets = 0, foundSize = 0;

        foreach (Media.Rtcp.RtcpPacket packet in Media.Rtcp.RtcpPacket.GetPackets(example, 0, example.Length))
        {
            ++foundPackets;

            foundSize += packet.Length;
        }

        if(foundPackets != 2) throw new Exception("Unexpected amount of packets found");

        if (foundSize != example.Length) throw new Exception("Unexpected total length of packets found");

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
            output = rr.Prepare().ToArray();//should be exactly equal to example's bytes when extension data is contained in the packet instance

            //Use to get the raw data in the packet including the header
            //.Take(rr.Length - rr.ExtensionDataOctets) 

            //Or rr.ReportData which omits the header...

            //What other variations are relvent? Please submit examples, even invalid ones will be tolerated!

            //The bytes given here should reflect exactly the bytes in the example array because of how the data is formatted.

            //Yes the data is compound but there is an invalid LengthInWords in the packet which must be exactly copied when deserialized
            if (rr.HasExtensionData && false == output.SequenceEqual(example)) throw new Exception("Result Packet Does Not Match Example");
            else output = rr.Prepare(true, true, false, true).ToArray();
            for (int i = 0, e = output.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example @" + i);

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
            if (false == sourceDescription.HasCName) throw new Exception("Unexpected HasCName");

            if (sourceDescription.BlockCount != 1) throw new Exception("Unexpected BlockCount");

            if (sourceDescription.Chunks.First().ChunkIdentifer != 1777498448) throw new Exception("Chunks.ChunkIdentifer");

            if (false == sourceDescription.Chunks.First().HasItems) throw new Exception("Chunks.HasItems");

            if (sourceDescription.Chunks.First().Items.First().ItemType != Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName) throw new Exception("Unexpected ItemType");

            if (sourceDescription.Chunks.First().Items.First().ItemLength != 6) throw new Exception("Unexpected ItemLength");

            foreach (Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk chunk in sourceDescription.GetChunkIterator())
            {
                if (chunk.ChunkIdentifer != 1777498448) throw new Exception("Chunks.ChunkIdentifer");

                Console.WriteLine(string.Format(TestingFormat, "Chunk Identifier", chunk.ChunkIdentifer));

                //Use a SourceDescriptionItemList to access the items within the Chunk
                //This is performed auto magically when using the foreach pattern
                foreach (Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem item in chunk /*.AsEnumerable<Rtcp.SourceDescriptionItem>()*/)
                {
                    //if (item.ItemType != Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName) throw new Exception("Unexpected ItemType");

                    //if (item.ItemLength != 6) throw new Exception("Unexpected ItemLength");

                    Console.WriteLine(string.Format(TestingFormat, "Item Type", item.ItemType));

                    Console.WriteLine(string.Format(TestingFormat, "Item Length", item.ItemLength));

                    Console.WriteLine(string.Format(TestingFormat, "Item Data", BitConverter.ToString(item.ItemData.ToArray())));
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
        if (false == app.Name.SequenceEqual(System.Text.Encoding.UTF8.GetBytes("qtsi"))) throw new Exception("Invalid App Packet Type");

        //Check the length
        if (rtcpPacket.Length != example.Length) throw new Exception("Invalid Legnth");

        //Verify ApplicationSpecificReport byte for byte
        output = rtcpPacket.Prepare().ToArray();//should be exactly equal to example
        for (int i = 0, e = example.Length; i < e; ++i) if (example[i] != output[i]) throw new Exception("Result Packet Does Not Match Example");

        //Test making a packet with a known length in bytes
        Media.Rtcp.SourceDescriptionReport sd = new Media.Rtcp.SourceDescriptionReport(2);
        byte[] sdOut = sd.Prepare().ToArray();

        //1 word when the ssrc is present but would be an invalid sdes because blockCount = 0
        if (false == sd.IsComplete || sd.Length != Media.Rtcp.RtcpHeader.Length || sd.Header.LengthInWordsMinusOne != ushort.MaxValue) throw new Exception("Invalid Length");

        //Create 9 bytes of data to add to the existing SourceDescriptionReport
        byte[] itemData = System.Text.Encoding.UTF8.GetBytes("FLABIA-PC");

        int KnownId = 0x1AB7C080;

        //Point the rtcpPacket at the SourceDescription instance
        rtcpPacket = sd;

        //Create a Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk containing a Known Identifier
        //Which Contains a Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem with the Type 'CName' containing the itemData
        //Add the Media.Rtcp.IReportBlock to the RtcpReport
        sd.Add((Media.Rtcp.IReportBlock)new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk(KnownId,
            new Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem(Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName,
                itemData.Length, itemData, 0))); // ItemType(End) = 1, ItemLength(9) = 1, ItemData(9) = 11 Bytes in the Item, ChunkIdentifier(0x1AB7C080) = 4, 15 total bytes

        //Add an unpadded item for a 19 byte packet.
        //sd.Add(new Media.Rtcp.SourceDescriptionReport.SourceDescriptionChunk(KnownId,
        //    new Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem(Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName,
        //        itemData.Length, itemData, 0)), false); // ItemType(End) = 1, ItemLength(9) = 1, ItemData(9) = 11 Bytes in the Item, ChunkIdentifier(0x1AB7C080) = 4, 15 total bytes

        //Ensure the data is present where it is supposed to be, more data may be present to respect octet alignment
        if (false == sd.RtcpData.Skip(Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.ItemHeaderSize).Take(itemData.Length).SequenceEqual(itemData)) throw new Exception("Invalid ItemData");

        if (false == sd.Chunks.First().HasItems) throw new Exception("Unexpected HasItems");

        if (sd.Chunks.First().ChunkIdentifer != KnownId) throw new Exception("Unexpected Chunks.ChunkIdentifer");

        if (sd.Chunks.First().Items.Count() != 2) throw new Exception("Unexpected Chunks.Items.Count");

        if (sd.Chunks.First().Items.First().ItemType != Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.CName) throw new Exception("Unexpected Items.ItemType");

        if (sd.Chunks.First().Items.First().ItemLength != 9) throw new Exception("Unexpected Chunks.Items.ItemLength");

        if (false == sd.Chunks.First().Items.First().ItemData.SequenceEqual(itemData)) throw new Exception("Unexpected Chunks.Items.ItemData");

        if (sd.Chunks.First().Items.Last().ItemType != Media.Rtcp.SourceDescriptionReport.SourceDescriptionItem.SourceDescriptionItemType.End) throw new Exception("Unexpected Items.ItemType");

        if (sd.Chunks.First().Items.Last().ItemLength != 1) throw new Exception("Unexpected Chunks.Items.ItemLength");

        if (false == sd.Chunks.First().Items.Last().ItemData.All(b => b == 0)) throw new Exception("Unexpected Chunks.Items.ItemData");

        //
        // Header = 4 Bytes, 1 Word
        // There is a SSRC which occupies 1 Word
        //in a SourceDescription, The First Chunk is `Overlapped` in the header and the BlockIdentifier is shared with the SSRC

        //Ensure the data is present where it is supposed to be
        if (sd.SynchronizationSourceIdentifier != KnownId) throw new Exception("Invalid SynchronizationSourceIdentifier");

        //asPacket now contains 11 octets in the payload.
        //asPacket now has 1 block (1 chunk of 15 bytes)
        //asPacket is 19 octets long, 11 octets in the payload and 8 octets in the header
        //asPacket would have a LengthInWordsMinusOne of 3 because 19 / 4 = 4 - 1 = 3
        //But null octets are added (Per RFC3550 @ Page 45 [Paragraph 2] / http://tools.ietf.org/html/rfc3550#appendix-A.4)
        //19 + 1 = 20, 20 / 4 = 5, 5 - 1 = 4.
        if (false == rtcpPacket.IsComplete || rtcpPacket.Length != 20 || rtcpPacket.Header.LengthInWordsMinusOne != 4) throw new Exception("Invalid Length");
    }
}