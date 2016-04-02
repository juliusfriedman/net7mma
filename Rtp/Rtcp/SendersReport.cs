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
    #region SendersReport

    /// <summary>
    /// Provides a managed implemenation of the SendersReport abstraction outlined in http://tools.ietf.org/html/rfc3550#section-6.4.1
    /// </summary>
    public class SendersReport : RtcpReport
    {
        #region Constants and Statics

        public const int SendersInformationSize = 20;

        new public const int PayloadType = 200;

        #endregion

        #region Constructor

        public SendersReport(int version, int reportBlocks, int ssrc)
            : base(version, PayloadType, 0, ssrc, reportBlocks, ReportBlock.ReportBlockSize, SendersInformationSize)
        {

        }

        public SendersReport(int version, int padding, int reportBlocks, int ssrc)
            : base(version, PayloadType, padding, ssrc, reportBlocks, ReportBlock.ReportBlockSize, SendersInformationSize)
        {

        }

        /// <summary>
        /// Constructs a new SendersReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public SendersReport(RtcpHeader header, System.Collections.Generic.IEnumerable<byte> payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 200.", "reference");
        }

        /// <summary>
        /// Constructs a new SendersReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        public SendersReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 200.", "reference");
        }

        public SendersReport(RtcpPacket reference, bool shouldDispose = true)
            : base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 200.", "reference");
        }

        //Other overloads

        #endregion

        #region Properties

        #region Senders Information Properties

        /// <summary>
        /// The Most Significant Word (32 bit value) of the NTP Timestamp
        /// </summary>
        public int NtpMSW
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian); }
            internal protected set { Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The Least Significant Word (32 bit value) of the NTP Timestamp
        /// </summary>
        public int NtpLSW
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 4, BitConverter.IsLittleEndian); }
            internal protected set { Binary.Write32(Payload.Array, Payload.Offset + 4, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>            
        ///Corresponds to the same time as the NTP timestamp (above), but in the same units and with the same random offset as the RTP timestamps in data packets.  
        ///This correspondence may be used for intra- and inter-media synchronization for sources whose NTP timestamps are synchronized, and may be used by media-independent receivers to estimate the nominal RTP clock frequency.  
        ///
        ///Note that in most cases this timestamp will not be equal to the RTP timestamp in any adjacent data packet.  
        ///Rather, it MUST be calculated from the corresponding NTP timestamp using the relationship between the RTP timestamp counter and real time as maintained by periodically checking the wallclock time at a sampling instant.              
        /// </summary>
        public int RtpTimestamp
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 8, BitConverter.IsLittleEndian); }
            internal protected set { Binary.Write32(Payload.Array, Payload.Offset + 8, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        ///  The total number of RTP data packets transmitted by the sender since starting transmission up until the time this SR packet was generated.  
        ///  The count SHOULD be reset if the sender changes its SSRC identifier.
        /// </summary>
        public int SendersPacketCount
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 12, BitConverter.IsLittleEndian); }
            internal protected set { Binary.Write32(Payload.Array, Payload.Offset + 12, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The total number of payload octets (i.e., not including header or padding) transmitted in RTP data packets by the sender since starting transmission up until the time this SR packet was generated.  
        /// The count SHOULD be reset if the sender changes its SSRC identifier. 
        /// This field can be used to estimate the average payload data rate.
        /// </summary>
        public int SendersOctetCount
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 16, BitConverter.IsLittleEndian); }
            internal protected set { Binary.Write32(Payload.Array, Payload.Offset + 16, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// Calculates the system endian representation of the NtpTimestamp.
        /// </summary>
        /// <remarks>
        /// The value is stored in Network Byte Order
        /// </remarks>
        public long NtpTimestamp
        {
            get
            {
                if (BitConverter.IsLittleEndian) return (long)((ulong)NtpMSW << 32 | (uint)NtpLSW);

                return (long)((ulong)NtpLSW << 32 | (uint)NtpMSW);
            }
            internal protected set
            {

                //We need an unsigned representation of the value
                ulong unsigned = (ulong)value;

                if (BitConverter.IsLittleEndian)
                {

                    //Truncate the last 32 bits of the value, put the result in the LSW
                    NtpLSW = (int)unsigned;

                    //Move the value right 32 bits and put the result in the MSW
                    NtpMSW = (int)(unsigned >>= 32);
                }
                else
                {
                    //Truncate the last 32 bits of the value, put the result in the MSW
                    NtpMSW = (int)unsigned;

                    //Move the value right 32 bits and put the result in the LSW
                    NtpLSW = (int)(unsigned >>= 32);
                }
            }
        }

        /// <summary>
        /// Gets or Sets the DateTime representation of the NtpTimestamp.
        /// </summary>
        public DateTime NtpDateTime
        {
            get { return Media.Ntp.NetworkTimeProtocol.NptTimestampToDateTime((ulong)NtpTimestamp); }
            internal protected set { NtpTimestamp = (long)Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(ref value); }
        }

        #endregion

        //Calulcated correctly without the override
        //public override int ReportBlockOctets
        //{
        //    get
        //    {
        //        return base.ReportBlockOctets - SendersInformationSize;
        //    }
        //}

        /// <summary>
        /// Retrieves the the segment of data which corresponds to any ReportBlocks contained in the SendersReport after the SendersInformation.
        /// </summary>
        public override IEnumerable<byte> ReportData
        {
            get
            {
                if (false == HasReports || IsDisposed) return Common.MemorySegment.Empty;
                
                //return Payload.Skip(SendersInformationSize).Take(ReportBlockOctets);

                return new Common.MemorySegment(Payload.Array, Payload.Offset + SendersInformationSize, ReportBlockOctets);

            }
        }

        /// <summary>
        /// Generates a sequence of octets from the Payload which consist of the binary data contained in the Payload which corresponds to the SendersInformation.
        /// These sequence generates is constantly <see cref="SendersInformationSize"/> octets.
        /// </summary>
        public IEnumerable<byte> SendersInformation
        {
            get
            {
                //Used to use Take to allow this to proceed without an exception
                //if (Payload.Count < SendersInformationSize) return Common.MemorySegment.Empty;

                return new Common.MemorySegment(Payload.Array, Payload.Offset, SendersInformationSize);
            }
        }

      
        #endregion

        internal protected override IEnumerator<IReportBlock> GetEnumeratorInternal(int offset = 0)
        {
            //The SendersReport ReportBlocks start after the SendersInformation
            return base.GetEnumeratorInternal(offset + SendersInformationSize);
        }
    }

    #endregion
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the SendersReport class is correct
    /// </summary>
    internal class RtcpSendersReportUnitTests
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
                    using (Media.Rtcp.SendersReport p = new Rtcp.SendersReport(0, PaddingCounter, ReportBlockCounter, RandomId))
                    {
                        //Check SendersInformation
                        System.Diagnostics.Debug.Assert(p.SendersInformation.Count() == Rtcp.SendersReport.SendersInformationSize && p.SendersInformation.All(s=> s== 0), "Unexpected SendersInformation");

                        //Check IsComplete
                        System.Diagnostics.Debug.Assert(p.IsComplete, "IsComplete must be true.");

                        //Check Length
                        System.Diagnostics.Debug.Assert(p.Length == Binary.BitsPerByte + Rtcp.ReportBlock.ReportBlockSize * ReportBlockCounter + Rtcp.SendersReport.SendersInformationSize + PaddingCounter, "Unexpected Length");

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
                        using (Rtcp.SendersReport s = new Rtcp.SendersReport(new Rtcp.RtcpPacket(p.Prepare().ToArray(), 0), true))
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

                            //Check SendersInformation
                            System.Diagnostics.Debug.Assert(s.SendersInformation.Count() == Rtcp.SendersReport.SendersInformationSize && s.SendersInformation.All(o => o == 0), "Unexpected SendersInformation");
                        }
                    }
                }
            }
        }

        //Test AddReports And Enumerator And Reserialization
    }
}