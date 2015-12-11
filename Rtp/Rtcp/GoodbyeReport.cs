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
    #region GoodbyeReport

    /// <summary>
    /// Provides an implementation of the Goodbye Rtcp Packet outlined in http://tools.ietf.org/html/rfc3550#section-6.6
    /// </summary>
    public class GoodbyeReport : RtcpReport
    {

        #region Constants and Statics

        public new const int PayloadType = 203;

        #endregion

        /// <summary>
        /// Constructs a new GoodbyeReport from the given values.
        /// </summary>
        /// <param name="version">The version of the report</param>
        /// <param name="padding"></param>
        /// <param name="ssrc">The id of the senders of the report</param>
        /// <param name="sourcesLeaving">The SourceList which describes the sources who are leaving</param>
        /// <param name="reasonForLeaving">An optional reason for leaving(only the first 255 octets will be used)</param>
        public GoodbyeReport(int version, int padding, int ssrc, Media.RFC3550.SourceList sourcesLeaving, byte[] reasonForLeaving)
            : base(version, PayloadType, padding, ssrc, 
            sourcesLeaving != null ? sourcesLeaving.Count : 0,  //BlockCount
            Media.RFC3550.SourceList.ItemSize, //Size in bytes of each block
            Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(reasonForLeaving) ? 0 : 1 + reasonForLeaving.Length) //Extension size in bytes
        {

            //The working offset in the payload
            int offset = Payload.Offset;

            //Copy the source list (IMHO it should be before the reason...)
            if (sourcesLeaving != null)
            {
                sourcesLeaving.TryCopyTo(Payload.Array, offset);

                offset += sourcesLeaving.Size;
            }

            #region Babble

            /* If I won't have a participant list then I sure as shit won't make thing as they shouldn't be here just for [Wireshark, et al]
             6.6 BYE: Goodbye RTCP Packet

               0                   1                   2                   3
               0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
              +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              |V=2|P|    SC   |   PT=BYE=203  |             length            |
              +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              |                           SSRC/CSRC                           |
              +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              :                              ...                              : (THIS IS WHERE THE SOURCE LIST GOES FYI)
              +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
        (opt) |     length    |               reason for leaving            ...
              +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
             * When SC = 0 the packet is useless.
             * When SC >= 1 the SourceList appears BEFORE the (opt) Length, The id there may be different from SSRC/CSRC
             * Length is optional and a 0 value should not HAVE to be present as including a 0 value forces you to inlcude 3 more 0's to octet align the payload
             */

            #endregion

            //If a reason was given there will be extension data

            if (HasExtensionData)
            {
                int extensionLength = Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(reasonForLeaving) ? 0 : reasonForLeaving.Length;

                if (extensionLength > 0)
                {
                    //Ensure it will fit
                    if (extensionLength > byte.MaxValue) throw new InvalidOperationException("Only 255 octets can occupy the ReasonForLeaving in a GoodbyeReport.");

                    //The length before the string
                    Payload.Array[offset++] = (byte)extensionLength;

                    //Copy it to the payload
                    reasonForLeaving.CopyTo(Payload.Array, offset);
                }
            }
        }

        public GoodbyeReport(int version, int ssrc, Media.RFC3550.SourceList sourcesLeaving, byte[] reasonForLeaving)
            : this(version, 0, ssrc, sourcesLeaving, reasonForLeaving)
        {

        }

        public GoodbyeReport(int version, int ssrc, byte[] reasonForLeaving)
        //    : this(version, ssrc, null, reasonForLeaving) { } //Todo chain?
            : base(version, PayloadType, 0, ssrc, 0, 0, RtcpHeader.DefaultLengthInWords + 1, Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(reasonForLeaving) ? 0 : 1 + reasonForLeaving.Length)
        {
            int offset = Payload.Offset;

            if (HasExtensionData)
            {
                int extensionLength = Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(reasonForLeaving) ? 0 : reasonForLeaving.Length;

                if (extensionLength > 0)
                {
                    //Ensure it will fit
                    if (extensionLength > byte.MaxValue) throw new InvalidOperationException("Only 255 octets can occupy the ReasonForLeaving in a GoodbyeReport.");

                    //The length before the string
                    Payload.Array[offset++] = (byte)extensionLength;

                    //Copy it to the payload
                    reasonForLeaving.CopyTo(Payload.Array, offset);
                }
            }
        }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given values.
        /// </summary>
        /// <param name="version">The version of the report</param>
        /// <param name="ssrc">The id of the senders of the report</param>
        public GoodbyeReport(int version, int ssrc)
            //: base(new RtcpHeader(version, PayloadType, false, 0, ssrc), Common.MemorySegment.Empty) { }
            : base(version, PayloadType, 0, ssrc, 0, 0, RtcpHeader.DefaultLengthInWords, 0) { }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public GoodbyeReport(RtcpHeader header, IEnumerable<byte> payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
        }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        public GoodbyeReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
        }

        /// <summary>
        /// Constructs a GoodbyeReport instance from an existing RtcpPacket reference.
        /// Throws a ArgumentNullException if reference is null.
        /// Throws an ArgumentException if the <see cref="RtcpHeader.PayloadType"/> is not GoodbyeReport (203)
        /// </summary>
        /// <param name="reference">The packet containing the GoodbyeReport</param>
        public GoodbyeReport(RtcpPacket reference, bool shouldDispose = true)
            : this(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 203.", "reference");
        }

        /// <summary>
        /// Calculates the amount of octets contained in the Payload which belong to the SourceList.
        /// The BlockCount is obtained from the Header.
        /// </summary>
        public override int ReportBlockOctets { get { return RFC3550.SourceList.ItemSize * Header.BlockCount; } }

        /// <summary>
        /// Indicates if the GoodbyeReport contains a ReasonForLeaving based on the length of SourceList contained in the GoodbyeReport.
        /// </summary>
        public bool HasReasonForLeaving { get { return ReasonLength > 0; } }

        /// <summary>
        /// Gets the data assoicated with the ReasonForLeaving denoted by the length of field if present.
        /// If no reason for leaving is present then an empty sequence is returned.
        /// </summary>
        public virtual IEnumerable<byte> ReasonForLeaving
        {
            get
            {
                if (IsDisposed || false == HasReasonForLeaving) return Enumerable.Empty<byte>();

                return ExtensionData.Skip(1).Take(ReasonLength);
            }
        }

        /// <summary>
        /// Returns the length of the ReasonForLeaving field as indicated by the length of the field if present.
        /// If no reason for leaving is present 0 is returned.
        /// </summary>
        public virtual int ReasonLength
        {
            get
            {
                return HasExtensionData ? ExtensionData.First() : 0;
            }
        }

        #region Methods

        /// <summary>
        /// Creates a <see cref="SourceList"/> from the information contained in the GoodbyeReport.
        /// </summary>
        /// <returns>The <see cref="SourceList"/> created.</returns>
        public Media.RFC3550.SourceList GetSourceList() { return new Media.RFC3550.SourceList(this); }

        //Packet has clone
        ///// <summary>
        ///// Clones this GoodbyeReport instance.
        ///// If reference is true changes in either instance will be reflected in both.
        ///// </summary>
        ///// <param name="reference">Indicates if the new instance should reference this instance.</param>
        ///// <returns>The newly created instance.</returns>
        //public GoodbyeReport Clone(bool reference)
        //{
        //    //Todo, update to includeSourceList etc.
        //    if (reference) return new GoodbyeReport(Header, Payload);
        //    return new GoodbyeReport(Header.Clone(), Prepare().ToArray());
        //}

        #endregion

        public override void Add(IReportBlock reportBlock)
        {
            //Will throw an InvalidCastException is the given reportBlock is not a RFC3550.SourceList
            if (reportBlock is RFC3550.SourceList) Add(reportBlock as RFC3550.SourceList);
            else base.Add(reportBlock);
        }

        /// <summary>
        /// Adds the maximum amount of items from the source list as there are availabe blocks
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        internal virtual protected void Add(RFC3550.SourceList sourceList, int offset, int count)
        {
            if (sourceList == null) return;

            if (IsReadOnly) throw new InvalidOperationException("A RFC3550.SourceList cannot be added when IsReadOnly is true.");

            int reportBlocksRemaining = ReportBlocksRemaining;

            if (reportBlocksRemaining == 0) throw new InvalidOperationException("A RtcpReport can only hold 31 ReportBlocks");

            //Add the bytes to the payload and set the LengthInWordsMinusOne and increase the BlockCount
            
            //This is not valid when there is a ReasonForLeaving, the data needs to be placed before such reason and padding
            AddBytesToPayload(sourceList.AsBinaryEnumerable(offset, BlockCount += Binary.Min(reportBlocksRemaining, count)));
        }

        public virtual void Add(RFC3550.SourceList sourceList)
        {
            Add(sourceList, 0, sourceList.Count);
        }

        public override IEnumerator<IReportBlock> GetEnumerator()
        {
            using (RFC3550.SourceList sl = GetSourceList())
            {
                foreach (uint ssrc in sl)
                {
                    //Give a ReportBlock of 4 bytes to represent the source list entry
                    using (ReportBlock rb = new ReportBlock(new Common.MemorySegment(Payload.Array, Payload.Offset + sl.ItemIndex * RFC3550.SourceList.ItemSize, RFC3550.SourceList.ItemSize)))
                    {
                        yield return rb;
                    }
                }
            }
        }
    }

    #endregion
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the GoodbyeReport class is correct
    /// </summary>
    internal class RtcpGoodbyeReportUnitTests
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
                    //Enumerate every possible reason length
                    for (byte ReasonLength = byte.MinValue; ReasonLength <= Media.Common.Binary.FiveBitMaxValue; ++ReasonLength)
                    {
                        //Create the RandomId and ReasonForLeaving
                        
                        int RandomId = RFC3550.Random32(Utility.Random.Next());
                        
                        IEnumerable<byte> ReasonForLeaving = Array.ConvertAll(Enumerable.Range(1, (int)ReasonLength).ToArray(), Convert.ToByte);

                        //Create a GoodbyeReport instance using the specified options.
                        using (Media.Rtcp.GoodbyeReport p = new Rtcp.GoodbyeReport(0, PaddingCounter, RandomId, new RFC3550.SourceList(ReportBlockCounter), ReasonForLeaving.ToArray()))
                        {
                            //Check IsComplete
                            System.Diagnostics.Debug.Assert(p.IsComplete, "IsComplete must be true.");

                            //Check SynchronizationSourceIdentifier
                            System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == RandomId, "Unexpected SynchronizationSourceIdentifier");

                            //Calculate the length of the ReasonForLeaving, should always be padded to 32 bits for octet alignment.
                            int expectedReasonLength = ReasonLength > 0 ? Binary.BytesToMachineWords(ReasonLength + 1) * Binary.BytesPerInteger : 0;

                            //Check HasReasonForLeaving
                            System.Diagnostics.Debug.Assert(expectedReasonLength > 0 == p.HasReasonForLeaving, "Unexpected HasReasonForLeaving");

                            //Check the Payload.Count
                            System.Diagnostics.Debug.Assert(p.Payload.Count == ReportBlockCounter * Binary.BytesPerInteger + PaddingCounter + expectedReasonLength, "Unexpected Payload Count");

                            //Check the Length, 
                            System.Diagnostics.Debug.Assert(p.Length == p.Header.Size + ReportBlockCounter * Binary.BytesPerInteger + PaddingCounter + expectedReasonLength, "Unexpected Length");

                            //Check the BlockCount count
                            System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                            //Check the reaosn for leaving
                            System.Diagnostics.Debug.Assert(p.ReasonForLeaving.SequenceEqual(ReasonForLeaving), "Unexpected ReasonForLeaving data");

                            //Check the PaddingOctets count
                            System.Diagnostics.Debug.Assert(p.PaddingOctets == PaddingCounter, "Unexpected PaddingOctets");

                            //Check all data in the padding but not the padding octet itself.
                            System.Diagnostics.Debug.Assert(p.PaddingData.Take(PaddingCounter - 1).All(b => b == 0), "Unexpected PaddingData");

                            //Add remaining amount of reports to test the Add method

                            //Enumerate the RtcpReport version of the instance

                            //Serialize and Deserialize and verify again
                            using (Rtcp.GoodbyeReport s = new Rtcp.GoodbyeReport(new Rtcp.RtcpPacket(p.Prepare().ToArray(), 0), true))
                            {
                                //Check the Payload.Count
                                System.Diagnostics.Debug.Assert(s.Payload.Count == p.Payload.Count, "Unexpected Payload Count");

                                //Check the Length, 
                                System.Diagnostics.Debug.Assert(s.Length == p.Length, "Unexpected Length");

                                //Check the BlockCount count
                                System.Diagnostics.Debug.Assert(s.BlockCount == p.BlockCount, "Unexpected BlockCount");

                                //Check the reaosn for leaving
                                System.Diagnostics.Debug.Assert(s.ReasonForLeaving.SequenceEqual(ReasonForLeaving) && s.ReasonForLeaving.Count() == ReasonLength, "Unexpected ReasonForLeaving data");

                                //Check the PaddingOctets count
                                System.Diagnostics.Debug.Assert(s.PaddingOctets == p.PaddingOctets, "Unexpected PaddingOctets");

                                //Check all data in the padding but not the padding octet itself.
                                System.Diagnostics.Debug.Assert(s.PaddingData.SequenceEqual(p.PaddingData), "Unexpected PaddingData");
                            }
                        }
                    }
                }
            }
        }
    }
}