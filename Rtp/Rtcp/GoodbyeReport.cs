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
        /// <param name="ssrc">The id of the senders of the report</param>
        /// <param name="sourcesLeaving">The SourceList which describes the sources who are leaving</param>
        /// <param name="reasonForLeaving">An optional reason for leaving(only the first 255 octets will be used)</param>
        public GoodbyeReport(int version, int ssrc, Media.Rtp.RFC3550.SourceList sourcesLeaving, byte[] reasonForLeaving)
            : base(version, PayloadType, false, ssrc, sourcesLeaving != null ? sourcesLeaving.Count + 1 : 1, 0, reasonForLeaving != null ? reasonForLeaving.Length : 0)
        {

            //If a reason was given
            if (reasonForLeaving != null)
            {
                //Ensure it will fit
                if (reasonForLeaving.Length > byte.MaxValue) throw new InvalidOperationException("Only 255 octets can occupy the ReasonForLeaving in a GoodbyeReport.");

                //Copy it to the payload
                reasonForLeaving.CopyTo(Payload.Array, Payload.Offset);

                //http://tools.ietf.org/html/rfc3550#section-6.6
                /*
                 Optionally,
                   the BYE packet MAY include an 8-bit octet count followed by that many
                   octets of text indicating the reason for leaving, e.g., "camera
                   malfunction" or "RTP loop detected".  The string has the same
                   encoding as that described for SDES.  If the string fills the packet
                   to the next 32-bit boundary, the string is not null terminated.
                 */
                int nullOctetsRequired = 4 - (Payload.Count & 0x03);
                if (nullOctetsRequired > 0)
                {
                    //This will allow the data to end on a 32 bit boundary.
                    AddBytesToPayload(Enumerable.Repeat(SourceDescriptionReport.SourceDescriptionItem.Null, nullOctetsRequired), 0, nullOctetsRequired);
                }
            }            
        }

        public GoodbyeReport(int version, int ssrc, byte[] reasonForLeaving)
            : this(version, ssrc, null, reasonForLeaving) { }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given values.
        /// </summary>
        /// <param name="version">The version of the report</param>
        /// <param name="ssrc">The id of the senders of the report</param>
        public GoodbyeReport(int version, int ssrc) : base(version, PayloadType, false, ssrc, 0, 0, 0) { }

        public GoodbyeReport(int version, bool padding, int ssrc) : base(version, PayloadType, padding, ssrc, 0, 0, 0) { }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public GoodbyeReport(RtcpHeader header, IEnumerable<byte> payload)
            : base(header, payload)
        {
        }

        /// <summary>
        /// Constructs a new GoodbyeReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        public GoodbyeReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose)
            : base(header, payload, shouldDipose)
        {
        }

        /// <summary>
        /// Constructs a GoodbyeReport instance from an existing RtcpPacket reference.
        /// Throws a ArgumentNullException if reference is null.
        /// Throws an ArgumentException if the <see cref="RtcpHeader.PayloadType"/> is not GoodbyeReport (203)
        /// </summary>
        /// <param name="reference">The packet containing the GoodbyeReport</param>
        public GoodbyeReport(RtcpPacket reference, bool shouldDispose)
            :this(reference.Header, reference.Payload)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 203.", "reference");
        }

        /// <summary>
        /// Calculates the amount of octets contained in the Payload which belong to the SourceList.
        /// The BlockCount is obtained from the Header.
        /// </summary>
        public override int ReportBlockOctets { get { return !Disposed && HasReports ? 4 * Header.BlockCount : 0; } }

        /// <summary>
        /// Indicates if the GoodbyeReport contains a ReasonForLeaving based on the length of SourceList contained in the GoodbyeReport.
        /// </summary>
        public bool HasReasonForLeaving { get { return !Disposed && ExtensionData.Count() > 1; } }

        /// <summary>
        /// Gets the data assoicated with the ReasonForLeaving denoted by the length of field if present.
        /// If no reason for leaving is present then an empty sequence is returned.
        /// </summary>
        public IEnumerable<byte> ReasonForLeaving
        {
            get { if (Disposed || !HasReasonForLeaving) return Enumerable.Empty<byte>(); return ExtensionData.Skip(1).Take(ReasonLength); }
        }

        /// <summary>
        /// Returns the length of the ReasonForLeaving field as indicated by the length of the field if present.
        /// If no reason for leaving is present 0 is returned.
        /// </summary>
        public int ReasonLength
        {
            get
            {
                if (!Disposed && HasReasonForLeaving) return ExtensionData.Take(1).First();
                return 0;
            }
        }

        #region Methods

        /// <summary>
        /// Creates a <see cref="SourceList"/> from the information contained in the GoodbyeReport.
        /// </summary>
        /// <returns>The <see cref="SourceList"/> created.</returns>
        public Media.Rtp.RFC3550.SourceList GetSourceList() { if (Disposed) return null; return new Media.Rtp.RFC3550.SourceList(this); }

        /// <summary>
        /// Clones this GoodbyeReport instance.
        /// If reference is true changes in either instance will be reflected in both.
        /// </summary>
        /// <param name="reference">Indicates if the new instance should reference this instance.</param>
        /// <returns>The newly created instance.</returns>
        public GoodbyeReport Clone(bool reference)
        {
            //Todo, update to includeSourceList etc.
            if (reference) return new GoodbyeReport(Header, Payload);
            return new GoodbyeReport(Header.Clone(), this.Prepare().ToArray());
        }

        #endregion


    }

    #endregion
}
