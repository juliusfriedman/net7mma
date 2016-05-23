using System;
using System.Collections.Generic;
using Media.Rtcp;

namespace Media.Rtcp.Feedback
{
    public abstract class RtcpFeedbackReport : RtcpReport
    {
        #region Constructors

        public RtcpFeedbackReport(int version, int type, int format, int padding, int ssrc, int mssrc, byte[] feedbackControlInformation, bool shouldDispose = true)
            : base(version, type, padding, ssrc, /*format*/0, 0,
            feedbackControlInformation != null ? Common.Binary.BytesPerInteger + feedbackControlInformation.Length : Common.Binary.BytesPerInteger, shouldDispose)
        {
            //Set the FMT field
            BlockCount = format;

            //Write the MediaSynchronizationSourceIdentifier
            Common.Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)mssrc);

            //Copy the FCI Data
            if (feedbackControlInformation != null)
                feedbackControlInformation.CopyTo(Payload.Array, Payload.Offset + Common.Binary.BytesPerInteger);
        }

        /// <summary>
        /// Constructs a RtcpFeedbackReport instance from an existing RtcpPacket reference.
        /// Throws a ArgumentNullException if reference is null.
        /// Throws an ArgumentException if the <see cref="RtcpHeader.PayloadType"/> is not 205 or 206.
        /// </summary>
        /// <param name="reference">The packet containing the RtcpFeedbackReport</param>
        public RtcpFeedbackReport(RtcpPacket reference, bool shouldDispose = true)
            : base(reference.Header, reference.Payload, shouldDispose)
        {
            //Validate PayloadType
            if (Header.PayloadType < 205 || Header.PayloadType > 206) throw new ArgumentException("Header.PayloadType is not equal to the expected type.", "reference");

            //Validate Format
            switch (Format)
            {
                case Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType.Unassigned: throw new InvalidOperationException("PayloadType should be a known FeedbackControlInformationType other than Unassigned.");
            }
        }

        /// <summary>
        /// Constructs a new instance of a RtcpFeedbackReport containing the given Payload.
        /// Any changes to the header are immediately visible in this instance.
        /// Any changes to the payload are not reflected in this instance.
        /// </summary>
        /// <param name="header">The <see cref="RtcpHeader"/> of the instance</param>
        /// <param name="payload">The <see cref="RtcpPacket.Payload"/> of the instance.</param>
        public RtcpFeedbackReport(RtcpHeader header, IEnumerable<byte> payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
        }

        /// <summary>
        /// Constructs a new RtcpFeedbackReport from the given <see cref="RtcpHeader"/> and payload
        /// Any changes to the header or Payload are immediately visible in this instance.
        /// </summary>
        /// <param name="header">The <see cref="RtcpHeader"/> of the instance</param>
        /// <param name="payload">The <see cref="RtcpPacket.Payload"/> of the instance.</param>
        public RtcpFeedbackReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value contained in the 'FMT' field which is also the <see cref="RtcpHeader.BlockCount"/>.
        /// </summary>
        public Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType Format
        {
            get
            {
                return (Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType)BlockCount;
            }
            set
            {
                BlockCount = (byte)value;
            }
        }

        /// <summary>
        /// Gets or sets the MediaSynchronizationSourceIdentifier field.
        /// </summary>
        public int MediaSynchronizationSourceIdentifier
        {
            get
            {

                return (int)Common.Binary.ReadU32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian);
            }
            set
            {
                Common.Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)value);
            }
        }

        /// <summary>
        /// Gets the data which corresponds to the FeedbackReport without any padding.
        /// </summary>
        public IEnumerable<byte> FeedbackControlInformation
        {
            get { return new Common.MemorySegment(Payload.Array, Payload.Offset + Common.Binary.BytesPerInteger, Payload.Count - PaddingOctets - Common.Binary.BytesPerInteger); }
        }

        /// <summary>
        /// Calculates the amount of octets contained in the Payload which belong to any <see cref="FeedbackControlInformation"/>.
        /// </summary>
        public override int ReportBlockOctets
        {
            get
            {
                //The payload without padding and the MediaSynchronizationSourceIdentifier
                return Payload.Count - PaddingOctets - Common.Binary.BytesPerInteger;
            }
        }

        /// <summary>
        /// Retrieves the segment of data which corresponds to all <see cref="FeedbackControlInformation"/>. contained in the RtcpReport.
        /// </summary>
        public override IEnumerable<byte> ReportData
        {
            get
            {
                return FeedbackControlInformation;
            }
        }

        internal protected override IEnumerator<IReportBlock> GetEnumeratorInternal(int offset = 0)
        {
            //Add RtcpFeedbackReport's contain a MediaSynchronizationSourceIdentifier before any report data
            return base.GetEnumeratorInternal(offset + Common.Binary.BytesPerInteger);
        }

        #endregion

        //Should provide Feedback Report Blocks Enumerator?

        //Should override GetEnumeratorInternal to return the Feedback Report Blocks?
    }
}
