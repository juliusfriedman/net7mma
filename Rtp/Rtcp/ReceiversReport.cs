#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Common;

#endregion
namespace Media.Rtcp
{
    #region ReceiversReport

        /// <summary>
        /// Provides a managed implemenation of the ReceiversReport defined in http://tools.ietf.org/html/rfc3550#section-6.4.2
        /// </summary>
        public class ReceiversReport : RtcpReport
        {
            #region Constants and Statics

            new public const int PayloadType = 201;

            #endregion

            #region Constructor

            public ReceiversReport(int version, int padding, int reportBlocks, int ssrc)
                : base(version, PayloadType, padding, ssrc, reportBlocks, ReportBlock.ReportBlockSize) { }

            public ReceiversReport(int version, int reportBlocks, int ssrc)
                : base(version, PayloadType, 0, ssrc, reportBlocks, ReportBlock.ReportBlockSize) { }

            public ReceiversReport(RtcpPacket reference, bool shouldDispose = true)
                : base(reference.Header, reference.Payload, shouldDispose)
            {
                if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 201.", "reference");
            }

            //Other overloads

            #endregion

        }

        #endregion
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the ReceiversReport class is correct
    /// </summary>
    internal class RtcpReceiversReportUnitTests
    {

        /// <summary>
        /// O( )
        /// </summary>
        public static void TestAConstructor_And_Reserialization()
        {            
            //Permute every possible value in the 5 bit BlockCount
            for (byte ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
            {
                //Permute every possible value in the Padding field.
                for (byte PaddingCounter = byte.MinValue; PaddingCounter <= Media.Common.Binary.FiveBitMaxValue; ++PaddingCounter)
                {
                    //Create a random id
                    int RandomId = RFC3550.Random32(Utility.Random.Next());

                    //Create a SendersReport instance using the specified options.
                    using (Media.Rtcp.ReceiversReport p = new Rtcp.ReceiversReport(0, PaddingCounter, ReportBlockCounter, RandomId))
                    {
                        //Check IsComplete
                        System.Diagnostics.Debug.Assert(p.IsComplete, "IsComplete must be true.");

                        //Check Length
                        System.Diagnostics.Debug.Assert(p.Length == Binary.BitsPerByte + Rtcp.ReportBlock.ReportBlockSize * ReportBlockCounter + PaddingCounter, "Unexpected Length");

                        //Check SynchronizationSourceIdentifier
                        System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == RandomId, "Unexpected SynchronizationSourceIdentifier");

                        //Check the BlockCount count
                        System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                        //Check the PaddingOctets count
                        System.Diagnostics.Debug.Assert(p.PaddingOctets == PaddingCounter, "Unexpected PaddingOctets");

                        //Check all data in the padding but not the padding octet itself.
                        System.Diagnostics.Debug.Assert(p.PaddingData.Take(PaddingCounter - 1).All(b => b == 0), "Unexpected PaddingData");

                        //Verify all IReportBlock
                        foreach (Rtcp.IReportBlock rb in p)
                        {
                            System.Diagnostics.Debug.Assert(rb.BlockIdentifier == 0, "Unexpected ChunkIdentifier");

                            System.Diagnostics.Debug.Assert(rb.BlockData.All(b => b == 0), "Unexpected BlockData");

                            System.Diagnostics.Debug.Assert(rb.Size == Media.Rtcp.ReportBlock.ReportBlockSize, "Unexpected Size");
                        }

                        //Serialize and Deserialize and verify again
                        using (Rtcp.ReceiversReport s = new Rtcp.ReceiversReport(new Rtcp.RtcpPacket(p.Prepare().ToArray(), 0), true))
                        {
                            //Check SynchronizationSourceIdentifier
                            System.Diagnostics.Debug.Assert(s.SynchronizationSourceIdentifier == p.SynchronizationSourceIdentifier, "Unexpected SynchronizationSourceIdentifier");

                            //Check the Payload.Count
                            System.Diagnostics.Debug.Assert(s.Payload.Count == p.Payload.Count, "Unexpected Payload Count");

                            //Check the Length, 
                            System.Diagnostics.Debug.Assert(s.Length == p.Length, "Unexpected Length");

                            //Check the BlockCount count
                            System.Diagnostics.Debug.Assert(s.BlockCount == p.BlockCount, "Unexpected BlockCount");

                            //Verify all IReportBlock
                            foreach (Rtcp.IReportBlock rb in s)
                            {
                                System.Diagnostics.Debug.Assert(rb.BlockIdentifier == 0, "Unexpected ChunkIdentifier");

                                System.Diagnostics.Debug.Assert(rb.BlockData.All(b => b == 0), "Unexpected BlockData");

                                System.Diagnostics.Debug.Assert(rb.Size == Media.Rtcp.ReportBlock.ReportBlockSize, "Unexpected Size");
                            }

                            //Check the RtcpData
                            System.Diagnostics.Debug.Assert(p.RtcpData.SequenceEqual(s.RtcpData), "Unexpected RtcpData");

                            //Check the PaddingOctets
                            System.Diagnostics.Debug.Assert(s.PaddingOctets == p.PaddingOctets, "Unexpected PaddingOctets");

                            //Check all data in the padding but not the padding octet itself.
                            System.Diagnostics.Debug.Assert(s.PaddingData.SequenceEqual(p.PaddingData), "Unexpected PaddingData");
                        }

                    }
                }
            }
        }

        //Test AddReports And Enumerator And Reserialization
    }
}