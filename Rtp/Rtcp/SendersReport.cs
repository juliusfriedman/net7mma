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

        public SendersReport(int version, bool padding, int reportBlocks, int ssrc)
            : base(version, PayloadType, padding, ssrc, reportBlocks, ReportBlock.ReportBlockSize, SendersInformationSize)
        {

        }

        public SendersReport(RtcpPacket reference, bool shouldDispose)
            : base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 200.", "reference");
        }

        #endregion

        #region Properties

        #region Senders Information Properties

        /// <summary>
        /// The Most Significant Word (32 bit value) of the NTP Timestamp
        /// </summary>
        public int NtpMSW
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian); }
            internal protected set { Binary.WriteNetwork32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The Least Significant Word (32 bit value) of the NTP Timestamp
        /// </summary>
        public int NtpLSW
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 4, BitConverter.IsLittleEndian); }
            internal protected set { Binary.WriteNetwork32(Payload.Array, Payload.Offset + 4, BitConverter.IsLittleEndian, (uint)value); }
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
            internal protected set { Binary.WriteNetwork32(Payload.Array, Payload.Offset + 8, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        ///  The total number of RTP data packets transmitted by the sender since starting transmission up until the time this SR packet was generated.  
        ///  The count SHOULD be reset if the sender changes its SSRC identifier.
        /// </summary>
        public int SendersPacketCount
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 12, BitConverter.IsLittleEndian); }
            internal protected set { Binary.WriteNetwork32(Payload.Array, Payload.Offset + 12, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// The total number of payload octets (i.e., not including header or padding) transmitted in RTP data packets by the sender since starting transmission up until the time this SR packet was generated.  
        /// The count SHOULD be reset if the sender changes its SSRC identifier. 
        /// This field can be used to estimate the average payload data rate.
        /// </summary>
        public int SendersOctetCount
        {
            get { return (int)Binary.ReadU32(Payload.Array, Payload.Offset + 16, BitConverter.IsLittleEndian); }
            internal protected set { Binary.WriteNetwork32(Payload.Array, Payload.Offset + 16, BitConverter.IsLittleEndian, (uint)value); }
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
                if (BitConverter.IsLittleEndian) return (long)((ulong)NtpLSW << 32 | (uint)NtpMSW);
                return (long)((ulong)NtpMSW << 32 | (uint)NtpLSW);
            }
            internal protected set
            {

                //We need an unsigned representation of the value
                ulong unsigned = (ulong)value;

                //Truncate the last 32 bits of the value, put the result in the MSW
                NtpMSW = (int)unsigned;

                //Move the value right 32 bits and put the result in the LSW
                NtpLSW = (int)(unsigned >>= 32);
            }
        }

        /// <summary>
        /// Calculates a DateTime representation of the NtpTimestamp.
        /// If the NtpTimestamp would be 0 then DateTime UtcNow is returned.
        /// </summary>
        public DateTime NtpTime
        {
            get { return Utility.NptTimestampToDateTime((ulong)NtpTimestamp); }
            internal protected set { NtpTimestamp = (long)Utility.DateTimeToNptTimestamp(value); }
        }

        #endregion

        /// <summary>
        /// Retrieves the the segment of data which corresponds to any ReportBlocks contained in the SendersReport after the SendersInformation.
        /// </summary>
        public override IEnumerable<byte> ReportData
        {
            get { if (!HasReports) return Enumerable.Empty<byte>(); return Payload.Array.Skip(Payload.Offset + SendersInformationSize).Take(ReportBlockOctets); }
        }

        /// <summary>
        /// Generates a sequence of octets from the Payload which consist of the binary data contained in the Payload which corresponds to the SendersInformation.
        /// These sequence generates is constantly <see cref="SendersInformationSize"/> octets.
        /// </summary>
        public IEnumerable<byte> SendersInformation
        {
            get { return Payload.Array.Skip(Payload.Offset).Take(SendersInformationSize); }
        }

      
        #endregion

        internal override IEnumerator<IReportBlock> GetEnumeratorInternal(int offset = 0)
        {
            return base.GetEnumeratorInternal(SendersInformationSize);
        }

    }

    #endregion
}
